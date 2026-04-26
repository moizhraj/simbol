# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Run a single test project
dotnet test tests/Simbol.Core.Tests
dotnet test tests/Simbol.Engine.Tests

# Run a specific test by name (partial match)
dotnet test --filter "FullyQualifiedName~ValueSimulatorTests"

# Run the simulator
dotnet run --project src/Simbol.Cli -- run --config config/sample.json

# Generate a sample config
dotnet run --project src/Simbol.Cli -- init --output my-config.json

# Validate a config file
dotnet run --project src/Simbol.Cli -- validate --config my-config.json
```

## Architecture

Three-project layered architecture with no upward dependencies:

```
Simbol.Core      → pure models, config, simulation patterns (no BACnet dependency)
Simbol.Engine    → BACnet device/object factories, service handler, simulation loop
Simbol.Cli       → CLI entry point, Spectre.Console live dashboard, Serilog wiring
```

### Startup flow (`SimulatorHostedService`)

On `StartAsync`, `SimulatorHostedService` calls `BacnetDeviceFactory.CreateDevice` for each device in config, registers each `SimulatedDevice` with `BacnetServiceHandler`, then creates a `BacnetClient` (UDP transport) and calls `AttachClient` to wire up all BACnet service event handlers (Who-Is, RP, RPM, WP, SubscribeCOV).

A second hosted service, `SimulationEngine` (`BackgroundService`), drives a `PeriodicTimer` at `SimulationIntervalMs`. Each tick calls `TickAllDevices`, which iterates every `SimulatedObject`, checks its per-object jittered `UpdateIntervalMs`, and if due, calls `IValueSimulator.GetAnalogValue/GetBinaryValue/GetMultiStateValue` and writes the result into `DeviceStorage`. After writing, it calls `BacnetServiceHandler.SendCovNotifications` to push to any active COV subscribers.

### Key types

- **`SimulatedDevice`** — holds a `DeviceStorage` (the BACnet stack's in-memory property store), a list of `SimulatedObject`s, and a `DeviceStats` for the dashboard.
- **`SimulatedObject`** — links a `BacnetObjectId` to its `IValueSimulator`, tracks `IsOverridden` (set on WriteProperty to freeze simulation for that object), and carries its jittered `UpdateIntervalMs` and `LastUpdateTime`.
- **`BacnetDeviceFactory`** — static factory; `globalInstanceBase` is threaded through multi-device creation so BACnet instance numbers never collide across devices.
- **`IConsoleDisplay`** — interface injected into `BacnetServiceHandler` and `SimulatorHostedService`; `SpectreConsoleDisplay` (Cli) and `NullConsoleDisplay` (Engine tests) implement it.

### Simulation patterns (`Simbol.Core`)

`IValueSimulator` has three methods: `GetAnalogValue`, `GetBinaryValue`, `GetMultiStateValue`, all taking `elapsedSeconds` since simulation start. Each `SimulatedObject` is assigned a random phase offset at creation so objects with the same pattern produce different values. Implementations: `SineWaveSimulator`, `RampSimulator`, `SawtoothSimulator`, `RandomSimulator`, `StaticValueSimulator`. `ValueSimulatorFactory.Create` selects the right one from `SimulationPattern` enum.

### Override mode

When an external BACnet client writes `PROP_PRESENT_VALUE` via WriteProperty, `BacnetServiceHandler` sets `simObj.IsOverridden = true`. `SimulationEngine.TickAllDevices` skips overridden objects, so the written value persists until the device is restarted.

### COV

`CovManager` holds an in-memory list of `CovSubscription`s (thread-safe via `lock`). Subscriptions with `lifetime == 0` never expire (`ExpiresAt = DateTime.MaxValue`). `GetActiveSubscriptions` prunes expired entries on each call. COV notifications are fired from `SimulationEngine` on every value update, not only on change.

### Testing

Unit tests use xUnit + FluentAssertions. `Simbol.Core.Tests` covers config loading/validation and all simulator patterns. `Simbol.Engine.Tests` covers factories, `DeviceStats`, and `ObjectTypeMapper`. Runtime components (`BacnetServiceHandler`, `SimulationEngine`, `CovManager`, `SimulatorHostedService`) have no unit tests — they require a live BACnet stack.
