namespace Simbol.Cli;

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
        rows.Add(new Markup("[grey]  RP=ReadProperty  RPM=ReadPropMultiple  WP=WriteProperty  COVSb=SubscribeCOV (incl. resubscriptions)  COVNot=COV Notifications[/]"));
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
        table.AddColumn(new TableColumn("[bold]Req (cur/peak)/min[/]").RightAligned());

        if (devices == null || devices.Count == 0)
        {
            table.AddRow(new Markup("[grey]Waiting for devices...[/]"));
            return table;
        }

        foreach (var device in devices.Values)
        {
            var stats = device.Stats;
            var rate = stats.CurrentRequestsPerMinute;
            var peak = stats.PeakRequestsPerMinute;

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
                new Markup($"[{rateColor}]{rate:F1}[/][grey]/{peak:F1}[/]")
            );
        }

        return table;
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
