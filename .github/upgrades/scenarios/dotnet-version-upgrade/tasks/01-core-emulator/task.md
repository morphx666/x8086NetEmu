# 01-core-emulator: Upgrade the emulator core library

Upgrade `x8086NetEmu/x8086NetEmu.vbproj`, which is the main dependency root for the retained application path. This task covers converting the project to SDK style, retargeting it to `.NET 10`, and resolving the assessed binary, source, and behavioral compatibility issues so downstream projects can consume a modern core library.

## Scope Inventory
- **Project affected**: `x8086NetEmu/x8086NetEmu.vbproj`
- **Dependency position**: First project in topological order for the retained solution; referenced by `x8086NetEmuEto`, `x8086NetEmuRenderers`, `RunTests`, `RunTests2`, and `x8086NetEmuConsole`
- **Distinct concerns**:
  - SDK-style conversion from legacy VB project format
  - Target framework modernization for a dependency root that still has legacy downstream consumers
  - Replacement of a few framework-specific APIs (`My.Application.Info.DirectoryPath`, `Environment.OSVersion`, WMI access via `System.Management.ManagementObject`)
  - Preservation of native `ManagedBass` assets currently copied from the project tree

## Research Findings
- Assessment summary for this project reports 17 total issues: SDK-style conversion, target framework change, 6 binary incompatibilities, 3 source incompatibilities, and 6 behavioral warnings.
- `packages.config` contains a single dependency, `ManagedBass` 4.0.2, which the assessment marked compatible for modern .NET.
- The project file includes legacy VB application metadata (`MyType`, `Application.myapp`, designer-generated `My Project` files), a `System.Web` reference/import that appears unused in the retained core library, and explicit native BASS asset copy items.
- The concrete flagged code locations are concentrated in `Helpers/EmulatorState.vb`, `Helpers/Misc/HostRuntime.vb`, `Helpers/ConsoleCrayon.vb`, `Helpers/Helpers.vb`, and `Helpers/Properties.vb`.

## Execution Notes
- Convert the project to SDK style first without changing the legacy target framework, following the SDK conversion workflow.
- Because downstream retained projects are still on `.NET Framework 4.8.1`, use a bridge target framework strategy if needed so dependents can continue referencing the core while later tasks are upgraded.
- Favor modern platform detection APIs over `Environment.OSVersion` checks where possible, and replace `My.Application.Info.DirectoryPath` with a runtime-safe path source.
- Keep WinForms retirement out of this task; only preserve compatibility for the retained non-WinForms dependency chain.

**Done when**: `x8086NetEmu.vbproj` is SDK-style, targets `.NET 10`, its assessed compatibility issues are resolved, and dependent retained projects can reference it without target-framework conflicts.
