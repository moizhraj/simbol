using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Spectre.Console;
using Simbol.Cli;
using Simbol.Core.Configuration;
using Simbol.Engine.Services;

var configOption = new Option<string>("--config", "-c")
{
    Description = "Path to the JSON configuration file",
    Required = true
};

var verboseOption = new Option<bool>("--verbose", "-v")
{
    Description = "Enable verbose (debug) logging"
};

var quietOption = new Option<bool>("--quiet", "-q")
{
    Description = "Suppress informational output"
};

// --- run command ---
var runCommand = new Command("run", "Start the BACnet device simulator");
runCommand.Add(configOption);
runCommand.Add(verboseOption);
runCommand.Add(quietOption);
runCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var configPath = parseResult.GetValue(configOption)!;
    var verbose = parseResult.GetValue(verboseOption);
    var quiet = parseResult.GetValue(quietOption);

    var logLevel = verbose ? LogEventLevel.Debug : quiet ? LogEventLevel.Warning : LogEventLevel.Information;

    var logFileName = $"simbol-{DateTime.Now:yyyyMMdd-HHmmss}.log";
    var logFilePath = Path.Combine(Path.GetTempPath(), logFileName);

    var display = new SpectreConsoleDisplay();
    display.SetConfigPath(configPath);
    display.SetLogFilePath(logFilePath);

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Is(logLevel)
        .WriteTo.File(logFilePath,
            outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            flushToDiskInterval: TimeSpan.FromSeconds(1))
        .CreateLogger();

    try
    {
        var config = ConfigLoader.LoadFromFile(configPath);
        Log.Information("Configuration loaded: {DeviceCount} device(s) from {Path}", config.Devices.Count, configPath);

        var host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddSingleton(config);
                services.AddSingleton<IConsoleDisplay>(display);
                services.AddSingleton<RecentActivityBuffer>();
                services.AddSingleton<CovManager>();
                services.AddSingleton<BacnetServiceHandler>();
                services.AddHostedService<SimulatorHostedService>();
                services.AddHostedService<SimulationEngine>();
            })
            .Build();

        var hostTask = host.RunAsync(cancellationToken);
        var displayTask = display.RunAsync(cancellationToken);

        await Task.WhenAll(hostTask, displayTask);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        Log.Fatal(ex, "Simulator failed to start");
        AnsiConsole.MarkupLine($"[red]Fatal: {Markup.Escape(ex.Message)}[/]");
        return 1;
    }
    finally
    {
        await Log.CloseAndFlushAsync();
        AnsiConsole.MarkupLine($"\n[grey]Log file: {Markup.Escape(logFilePath)}[/]");
    }

    return 0;
});

// --- validate command ---
var validateCommand = new Command("validate", "Validate a configuration file without starting");
validateCommand.Add(configOption);
validateCommand.SetAction((parseResult) =>
{
    var configPath = parseResult.GetValue(configOption)!;

    try
    {
        var config = ConfigLoader.LoadFromFile(configPath);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Configuration is valid.");
        Console.ResetColor();
        Console.WriteLine($"  Devices: {config.Devices.Count}");
        foreach (var device in config.Devices)
        {
            var totalObjects = device.Objects.Values.Sum(g => g.Count);
            Console.WriteLine($"  - {device.Name} (ID: {device.InstanceId}): {totalObjects} objects");
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ Configuration is invalid: {ex.Message}");
        Console.ResetColor();
    }
});

// --- init command ---
var outputOption = new Option<string>("--output", "-o")
{
    Description = "Output path for the sample configuration file",
    DefaultValueFactory = _ => "simbol-config.json"
};

var initCommand = new Command("init", "Generate a sample configuration file");
initCommand.Add(outputOption);
initCommand.SetAction((parseResult) =>
{
    var outputPath = parseResult.GetValue(outputOption)!;
    var sampleJson = ConfigLoader.GenerateSampleConfig();
    File.WriteAllText(outputPath, sampleJson);
    Console.WriteLine($"Sample configuration written to: {outputPath}");
});

// --- root command ---
var rootCommand = new RootCommand("Simbol — Simulator for BACnet Object Layer");
rootCommand.Add(runCommand);
rootCommand.Add(validateCommand);
rootCommand.Add(initCommand);

var result = rootCommand.Parse(args);
return await result.InvokeAsync();
