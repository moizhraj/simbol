# Simbol — Simulator for BACnet Object Layer

A CLI-based BACnet/IP device simulator that spins up virtual BACnet devices from a JSON config file. Point it at a configuration, and Simbol creates fully functional BACnet devices on your network — complete with dynamic value simulation, COV subscriptions, and writable object support.

## Features

- **Config-driven simulation** — define devices and objects in a single JSON file
- **Multiple devices on one port** — run many BACnet device instances sharing a single UDP socket
- **9 BACnet object types** — AI, AO, AV, BI, BO, BV, MSI, MSO, MSV
- **Dynamic value patterns** — static, sine wave, ramp (triangle), random, sawtooth
- **Realistic per-object variation** — each object gets a random phase offset so values differ across objects even with the same pattern
- **Per-object update intervals** — configurable update rate per object group with automatic jitter spread for realistic timing
- **BACnet services** — Who-Is / I-Am, ReadProperty, ReadPropertyMultiple, WriteProperty, SubscribeCOV
- **Writable objects** — accept WriteProperty and pause simulation (override mode)
- **COV support** — Change of Value subscription and notification
- **Live load statistics** — per-device request counts, rates, unique clients, and peak tracking logged periodically
- **Graceful shutdown** — clean exit on Ctrl+C

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- No additional dependencies needed (BACnet NuGet package is restored automatically)

## Quick Start

```bash
# Clone and build
git clone <repo-url>
cd simbol
dotnet build

# Generate a sample config
dotnet run --project src/Simbol.Cli -- init --output my-config.json

# Validate config
dotnet run --project src/Simbol.Cli -- validate --config my-config.json

# Start simulator
dotnet run --project src/Simbol.Cli -- run --config my-config.json
```

## CLI Commands

| Command | Description |
|---------|-------------|
| `simbol run --config <path>` | Start the simulator. Flags: `--verbose`, `--quiet` |
| `simbol validate --config <path>` | Validate config file without starting devices |
| `simbol init [--output <path>]` | Generate a sample config (default: `simbol-config.json`) |

### Live Dashboard

When running `simbol run`, the console displays a **self-updating dashboard** (powered by Spectre.Console) instead of scrolling logs:

- **Header** — config file path, uptime, log file location
- **Device Load table** — per-device request stats refreshed every second
- **Recent Activity** — last 8 significant events (Who-Is, RPM, writes, COV subscriptions)

All detailed logs (debug, info, warning, error) are written to a **temp log file** at `%TEMP%\simbol-<timestamp>.log`. The path is displayed in the dashboard header and printed again on exit. Use `Get-Content -Wait` or `tail -f` to follow the log in a separate terminal.

## Configuration Reference

The config file has three top-level sections: `network`, `defaults`, and `devices`.

### Schema

```jsonc
{
  "network": {
    "interface": "0.0.0.0",         // Bind address (default: all interfaces)
    "port": 47808,                   // BACnet/IP UDP port (default: 0xBAC0)
    "broadcastAddress": "255.255.255.255"  // Broadcast address for Who-Is
  },

  "defaults": {
    "vendorId": 999,                 // Default vendor identifier
    "vendorName": "Simbol Simulator",// Default vendor name
    "simulationIntervalMs": 1000,    // Engine tick interval in milliseconds
    "updateIntervalMs": 5000,        // Default per-object update interval (ms)
    "jitterPercent": 50,             // Jitter range (0-100) applied to update intervals
    "statsIntervalSeconds": 5        // How often to log device load statistics
    "valueRange": { "min": 0.0, "max": 100.0 },  // Default value range
    "simulationPattern": "sine"      // Default simulation pattern
  },

  "devices": [
    {
      "instanceId": 1000,            // BACnet device instance number (unique)
      "name": "SimDevice-1",         // Device object name
      "description": "...",          // Device description
      "vendorId": 999,               // Optional — overrides defaults.vendorId
      "vendorName": "...",           // Optional — overrides defaults.vendorName

      "objects": {
        "<object-type>": {
          "count": 10,               // Number of objects to create
          "simulationPattern": "sine",  // Optional — overrides default
          "valueRange": { "min": 60.0, "max": 80.0 },  // Optional — overrides default
          "defaultValue": 72.0,      // Optional — initial/static value
          "numberOfStates": 4,       // Required for multi-state types
          "updateIntervalMs": 10000  // Optional — per-group update interval (ms)
        }
      }
    }
  ]
}
```

### Full Example

```json
{
  "network": {
    "interface": "0.0.0.0",
    "port": 47808,
    "broadcastAddress": "255.255.255.255"
  },
  "defaults": {
    "vendorId": 999,
    "vendorName": "Simbol Simulator",
    "simulationIntervalMs": 1000,
    "updateIntervalMs": 5000,
    "jitterPercent": 50,
    "statsIntervalSeconds": 5,
    "valueRange": { "min": 0.0, "max": 100.0 },
    "simulationPattern": "sine"
  },
  "devices": [
    {
      "instanceId": 1000,
      "name": "SimDevice-1",
      "description": "Simulated HVAC Controller",
      "objects": {
        "analog-input": { "count": 10, "simulationPattern": "sine", "valueRange": { "min": 60.0, "max": 80.0 }, "updateIntervalMs": 10000 },
        "analog-output": { "count": 5, "simulationPattern": "static", "defaultValue": 72.0 },
        "analog-value": { "count": 5 },
        "binary-input": { "count": 8, "simulationPattern": "random" },
        "binary-output": { "count": 4 },
        "binary-value": { "count": 4 },
        "multi-state-input": { "count": 3, "numberOfStates": 4 },
        "multi-state-output": { "count": 2, "numberOfStates": 3 },
        "multi-state-value": { "count": 2, "numberOfStates": 5 }
      }
    },
    {
      "instanceId": 1001,
      "name": "SimDevice-2",
      "description": "Simulated Lighting Controller",
      "objects": {
        "binary-output": { "count": 20 },
        "analog-value": { "count": 10 }
      }
    }
  ]
}
```

## Object Types

| Config Key | BACnet Type | Writable | Value Type |
|---|---|---|---|
| `analog-input` | ANALOG_INPUT | No | Float (REAL) |
| `analog-output` | ANALOG_OUTPUT | Yes | Float (REAL) |
| `analog-value` | ANALOG_VALUE | Yes | Float (REAL) |
| `binary-input` | BINARY_INPUT | No | Enumerated (0/1) |
| `binary-output` | BINARY_OUTPUT | Yes | Enumerated (0/1) |
| `binary-value` | BINARY_VALUE | Yes | Enumerated (0/1) |
| `multi-state-input` | MULTI_STATE_INPUT | No | Unsigned Int |
| `multi-state-output` | MULTI_STATE_OUTPUT | Yes | Unsigned Int |
| `multi-state-value` | MULTI_STATE_VALUE | Yes | Unsigned Int |

> **Override mode:** Writing to a writable object pauses its simulation pattern and holds the written value until the device is restarted.

## Simulation Patterns

| Pattern | Behavior | Good For |
|---|---|---|
| `static` | Constant value (`defaultValue` or midpoint of range) | Setpoint testing |
| `sine` | Sinusoidal oscillation over a 60 s period | Temperature, humidity simulation |
| `ramp` | Triangle wave (up then down) over a 60 s period | Gradual change testing |
| `random` | Random value each tick within the configured range | Noise / disturbance testing |
| `sawtooth` | Linear ramp then reset over a 60 s period | Accumulator simulation |

### Per-Object Variation

Each simulated object automatically receives a **random phase offset** so that objects sharing the same pattern produce different values at any given moment. For example, ten analog-input objects all configured with `sine` will each follow a sine curve but start at different points in the cycle — just like real sensors that were installed and powered on at different times.

### Update Intervals

Object values do not all change at the same time. Each object has its own **update interval** that controls how often its present value is recalculated:

- **`defaults.updateIntervalMs`** — the baseline update interval for all objects (default: `5000` ms).
- **`objects.<type>.updateIntervalMs`** — override the interval for a specific object group.
- **`defaults.jitterPercent`** — the jitter range applied to update intervals (default: `50`, meaning ±50%). Set to `0` to disable jitter and update all objects at exact intervals.
- **Auto-jitter** — every individual object gets the configured jitter applied to its baseline interval. A `jitterPercent` of `50` with a 5 000 ms interval produces per-object intervals between 2 500 ms and 7 500 ms, so objects never update in lockstep.

> **Tip:** For a realistic HVAC simulation, use `"updateIntervalMs": 10000` (10 s) for temperature sensors and `"updateIntervalMs": 30000` (30 s) for slower-changing values like humidity.

## Load Statistics

While the simulator runs, the dashboard updates device stats at the interval defined by `defaults.statsIntervalSeconds` (default: 5 s). The table shows **per-device** metrics:

| Column | Meaning |
|--------|---------|
| **Total** | Total inbound requests (lifetime) |
| **RP** | ReadProperty requests |
| **RPM** | ReadPropertyMultiple requests |
| **WP** | WriteProperty requests |
| **COVSb** | SubscribeCOV requests |
| **WhoIs** | Who-Is discovery hits |
| **COVNot** | Outbound COV notifications sent |
| **Errors** | Error responses sent |
| **Clients** | Unique client addresses seen |
| **Req/min** | Current rate / peak rate |

Use this to identify over-polling clients or uneven load distribution across devices.

## Architecture

Simbol is organized into three projects:

```
src/
├── Simbol.Core      Config models, value simulation strategies
├── Simbol.Engine     BACnet device/object factories, service handler, simulation tick engine
└── Simbol.Cli        CLI entry point, host wiring
```

- **Simbol.Core** — pure models and simulation logic with no BACnet dependency.
- **Simbol.Engine** — wires up BACnet devices, registers objects, handles incoming service requests, and drives the simulation loop.
- **Simbol.Cli** — parses command-line arguments and composes the runtime host.

## Testing

```bash
dotnet test
```

## License

TBD
