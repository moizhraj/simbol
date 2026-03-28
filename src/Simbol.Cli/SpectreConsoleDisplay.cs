namespace Simbol.Cli;

using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using Simbol.Engine.Models;
using Simbol.Engine.Services;

public class SpectreConsoleDisplay : IConsoleDisplay
{
    private readonly RecentActivityBuffer _activityBuffer = new(8);
    private string _logFilePath = "";
    private string _configPath = "";
    private IReadOnlyDictionary<uint, SimulatedDevice>? _devices;
    private TimeSpan _uptime;
    private int _statsIntervalSeconds = 30;
    private readonly object _lock = new();

    public void UpdateDashboard(IReadOnlyDictionary<uint, SimulatedDevice> devices, TimeSpan uptime, int statsIntervalSeconds)
    {
        lock (_lock)
        {
            _devices = devices;
            _uptime = uptime;
            _statsIntervalSeconds = statsIntervalSeconds;
        }
    }

    public void AddActivity(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _activityBuffer.Add($"[grey][[{timestamp}]][/] {Markup.Escape(message)}");
    }

    public void SetLogFilePath(string path) => _logFilePath = path;
    public void SetConfigPath(string path) => _configPath = path;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.Clear();

        await AnsiConsole.Live(BuildDisplay())
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ctx.UpdateTarget(BuildDisplay());
                    ctx.Refresh();
                    try
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            });
    }

    private IRenderable BuildDisplay()
    {
        var rows = new List<IRenderable>();

        rows.Add(BuildHeader());
        rows.Add(new Text(""));

        rows.Add(BuildStatsTable());
        rows.Add(new Markup("[grey]  RP=ReadProperty  RPM=ReadPropMultiple  WP=WriteProperty  COVSb=SubscribeCOV (incl. resubscriptions)  COVNot=COV Notifications  1m Peak=last 60s  All Peak=all-time[/]"));
        rows.Add(BuildSparklinePanel());
        rows.Add(new Text(""));

        rows.Add(BuildActivityPanel());
        rows.Add(new Text(""));

        rows.Add(new Markup("[grey]Press [bold]Ctrl+C[/] to stop[/]"));

        return new Rows(rows);
    }

    private IRenderable BuildHeader()
    {
        TimeSpan uptime;
        lock (_lock) { uptime = _uptime; }

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow(
            new Markup("[bold dodgerblue1]SIMBOL[/] [grey]— BACnet Simulator[/]"),
            new Markup($"[grey]Uptime:[/] [bold]{uptime:hh\\:mm\\:ss}[/]"));
        grid.AddRow(
            new Markup($"[grey]Config:[/] [italic]{Markup.Escape(_configPath)}[/]"),
            new Markup($"[grey]Log:[/] [italic link]{Markup.Escape(_logFilePath)}[/]"));

        return new Panel(grid)
            .Header("[bold] Simulator [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.DodgerBlue1)
            .Expand();
    }

    private IRenderable BuildStatsTable()
    {
        IReadOnlyDictionary<uint, SimulatedDevice>? devices;
        int statsInterval;
        lock (_lock)
        {
            devices = _devices;
            statsInterval = _statsIntervalSeconds;
        }

        var table = new Table()
            .Title("[bold] Device Load [/]")
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Expand();

        table.AddColumn(new TableColumn("[bold]Device[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Objects[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Total[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]RP[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]RPM[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]WP[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]COVSb[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]COVNot[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Errors[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Clients[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Req/min[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]1m Peak[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]All Peak[/]").RightAligned());

        if (devices == null || devices.Count == 0)
        {
            table.AddRow(new Markup("[grey]Waiting for devices...[/]"));
            return table;
        }

        foreach (var device in devices.Values)
        {
            var stats = device.Stats;
            var rate = stats.CurrentRequestsPerMinute;
            var rollingPeak = stats.RollingPeakPerMinute;
            var allTimePeak = stats.PeakRequestsPerMinute;

            var rateColor = rate > 60 ? "red" : rate > 30 ? "yellow" : "green";
            var errorColor = stats.ErrorCount > 0 ? "red" : "grey";

            table.AddRow(
                new Markup($"[bold]{Markup.Escape(device.Name)}[/]"),
                new Markup($"{device.SimulatedObjects.Count}"),
                new Markup($"[bold]{stats.TotalRequestCount}[/]"),
                new Markup($"{stats.ReadPropertyCount}"),
                new Markup($"{stats.ReadPropertyMultipleCount}"),
                new Markup($"{stats.WritePropertyCount}"),
                new Markup($"{stats.SubscribeCovCount}"),
                new Markup($"{stats.CovNotificationsSentCount}"),
                new Markup($"[{errorColor}]{stats.ErrorCount}[/]"),
                new Markup($"{stats.UniqueClientCount}"),
                new Markup($"[{rateColor}]{rate:F1}[/]"),
                new Markup($"[yellow]{rollingPeak:F1}[/]"),
                new Markup($"[grey]{allTimePeak:F1}[/]")
            );
        }

        return table;
    }

    private IRenderable BuildSparklinePanel()
    {
        IReadOnlyDictionary<uint, SimulatedDevice>? devices;
        lock (_lock) { devices = _devices; }

        if (devices == null || devices.Count == 0)
            return new Text("");

        var sparklines = new List<IRenderable>();

        var windows = new (string Label, TimeSpan Window)[]
        {
            ("Last 5 min", TimeSpan.FromMinutes(5)),
            ("Last 1 hour", TimeSpan.FromHours(1)),
            ("All time", TimeSpan.FromHours(24))
        };

        foreach (var (label, window) in windows)
        {
            var hasData = false;
            var deviceLines = new List<IRenderable>();

            foreach (var device in devices.Values)
            {
                var points = window == TimeSpan.FromHours(24)
                    ? device.Stats.RateHistory.GetAllPoints()
                    : device.Stats.RateHistory.GetPoints(window);

                if (points.Length < 2) continue;
                hasData = true;

                var rates = points.Select(p => p.Rate).ToArray();
                var maxRate = rates.Max();
                var peakInWindow = maxRate;
                var sparkline = BuildSparkline(rates, maxRate);

                var name = device.Name.Length > 14 ? device.Name[..14] : device.Name;
                deviceLines.Add(new Markup($"  [bold]{Markup.Escape(name)}[/] {sparkline} [grey]peak: {peakInWindow:F1}[/]"));
            }

            if (hasData)
            {
                sparklines.Add(new Markup($"  [underline]{label}[/]"));
                sparklines.AddRange(deviceLines);
            }
        }

        if (sparklines.Count == 0)
            return new Panel(new Markup("[grey italic]Collecting rate data...[/]"))
                .Header("[bold] Rate History [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey)
                .Expand();

        return new Panel(new Rows(sparklines))
            .Header("[bold] Rate History [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Expand();
    }

    private static string BuildSparkline(double[] values, double maxValue)
    {
        char[] blocks = { ' ', '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█' };

        // Limit to last 60 data points to fit terminal width
        if (values.Length > 60)
            values = values[^60..];

        var sb = new StringBuilder();
        foreach (var val in values)
        {
            int level = maxValue > 0
                ? (int)Math.Clamp(Math.Round(val / maxValue * 8), 0, 8)
                : 0;

            var color = val > 60 ? "red" : val > 30 ? "yellow" : "green";
            sb.Append($"[{color}]{blocks[level]}[/]");
        }

        return sb.ToString();
    }

    private IRenderable BuildActivityPanel()
    {
        var items = _activityBuffer.GetItems();

        var panel = new Panel(
            items.Length > 0
                ? new Rows(items.Select(i => new Markup(i)))
                : (IRenderable)new Markup("[grey italic]No activity yet...[/]"))
            .Header("[bold] Recent Activity [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Expand();

        return panel;
    }
}
