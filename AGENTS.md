# AGENTS.md

## Setup

- Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Restore dependencies: `dotnet restore`
- Build: `dotnet build`

## Running the simulator

```bash
dotnet run --project src/Simbol.Cli -- init --output my-config.json   # generate sample config
dotnet run --project src/Simbol.Cli -- validate --config my-config.json
dotnet run --project src/Simbol.Cli -- run --config my-config.json
```

## Testing

```bash
dotnet test                                                        # all tests
dotnet test tests/Simbol.Core.Tests                                # Core only
dotnet test tests/Simbol.Engine.Tests                              # Engine only
dotnet test --filter "FullyQualifiedName~<TestClassName>"          # single class
dotnet test --collect:"XPlat Code Coverage"                        # with coverage
```

Tests use xUnit + FluentAssertions. Runtime BACnet components (`BacnetServiceHandler`, `SimulationEngine`, `CovManager`, `SimulatorHostedService`) are not unit-tested — they require a live BACnet stack.

## Code style

- C# 12, .NET 8, nullable reference types enabled
- No comments unless the intent is non-obvious
- `init`-only properties on model types (`SimulatedDevice`, `SimulatedObject`)
- Static factory methods in `Factories/` (e.g. `BacnetDeviceFactory.CreateDevice`)
- New simulation patterns implement `IValueSimulator` in `Simbol.Core/Simulation/` and are registered in `ValueSimulatorFactory.Create`

## Architecture

Three projects with strict layering — no upward dependencies:

| Project | Responsibility |
|---|---|
| `Simbol.Core` | Config models, `IValueSimulator` implementations, `ValueSimulatorFactory` |
| `Simbol.Engine` | BACnet factories, `BacnetServiceHandler`, `SimulationEngine`, `CovManager` |
| `Simbol.Cli` | CLI entry point (`Program.cs`), Spectre.Console dashboard, Serilog wiring |

See `CLAUDE.md` for a detailed walkthrough of the startup flow, simulation tick loop, override mode, and COV subscription lifecycle.

## PR guidelines

- Branch from `master`; PRs target `master`
- Commit messages follow conventional commits: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`
- CI runs `dotnet build` + `dotnet test` on Ubuntu via GitHub Actions (`.github/workflows/dotnet.yml`)
