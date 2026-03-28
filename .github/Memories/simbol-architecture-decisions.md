# Memory: Simbol Architecture Decisions

## Metadata

- PatternId: MEM-001
- PatternVersion: 1
- Status: active
- Supersedes: none
- CreatedAt: 2026-03-28
- LastValidatedAt: 2026-03-28
- ValidationEvidence: Solution builds and runs successfully, simulator responds to BACnet discovery

## Source Context

- Triggering task: Initial Simbol project creation
- Scope/system: Simbol BACnet device simulator
- Date/time: 2026-03-28

## Memory

- Key fact or decision: Simbol uses a single BacnetClient (single UDP socket on port 47808) to multiplex multiple virtual BACnet device instances. Each device has its own DeviceStorage with programmatically-created objects. The System.IO.BACnet library (NuGet: BACnet v3.0.2 by ela-compil) is used as the BACnet protocol stack.
- Why it matters: This design avoids UDP port exhaustion when simulating many devices and matches how the library is designed to work. DeviceStorage objects and properties are created programmatically (not from XML) to support dynamic config-driven setup.

## Applicability

- When to reuse: Any future work extending Simbol — adding object types, new simulation patterns, multi-transport support, or modifying how devices are created.
- Preconditions/limitations:
  - BacnetBitString has no parameterized constructor; use field initializer syntax: `new BacnetBitString { bits_used = 4, value = new byte[] { 0x00 } }`
  - DeviceStorage.ReadPropertyAll returns `out IList<BacnetPropertyValue>` (not `IList<BacnetReadAccessResult>`)
  - System.CommandLine v3 preview uses `SetAction()` not `SetHandler()`, and `Option<T>.DefaultValueFactory` instead of `SetDefaultValue()`

## Actionable Guidance

- Recommended future action: When adding new BACnet object types, update ObjectTypeMapper, BacnetObjectFactory, and the valid types set in DeviceConfig.Validate()
- Related files/services/components:
  - `src/Simbol.Engine/Factories/ObjectTypeMapper.cs` — maps config strings to BacnetObjectTypes
  - `src/Simbol.Engine/Factories/BacnetObjectFactory.cs` — creates Storage.Object with properties
  - `src/Simbol.Engine/Factories/BacnetDeviceFactory.cs` — creates SimulatedDevice from config
  - `src/Simbol.Engine/Services/BacnetServiceHandler.cs` — handles BACnet service requests
  - `src/Simbol.Engine/Services/SimulationEngine.cs` — ticks value simulators
  - `src/Simbol.Engine/Services/IConsoleDisplay.cs` — display abstraction (Engine doesn't depend on Spectre)
  - `src/Simbol.Engine/Services/NullConsoleDisplay.cs` — no-op display for tests
  - `src/Simbol.Engine/Services/RecentActivityBuffer.cs` — thread-safe circular buffer
  - `src/Simbol.Cli/SpectreConsoleDisplay.cs` — Spectre.Console Live dashboard implementation
  - `src/Simbol.Core/Configuration/` — all config POCOs
  - `config/sample-config.json` — example configuration
