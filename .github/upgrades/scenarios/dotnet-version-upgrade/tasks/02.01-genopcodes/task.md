# 02.01-genopcodes: Upgrade the GenOpCodes utility

# 02.01-genopcodes: Upgrade the GenOpCodes utility

## Objective
Convert `GenOpCodes/GenOpCodes.vbproj` to SDK style and retarget it to modern .NET so the opcode-generation utility remains buildable in the retained solution.

## Scope
- `GenOpCodes/GenOpCodes.vbproj`
- `GenOpCodes/ModuleMain.vb` if any target-framework compatibility fixes are needed

## Research Findings
- `GenOpCodes` is a small standalone VB console tool with no project references and no assessed API incompatibilities.
- The current project is a classic VB console project targeting `.NET Framework 4.8.1` with legacy VB application/settings artifacts and no NuGet packages.
- The code in `ModuleMain.vb` primarily uses file I/O and debugger checks, so the expected work is structural project modernization plus target framework retargeting.

## Done when
- `GenOpCodes.vbproj` is SDK-style
- `GenOpCodes.vbproj` targets the planned modern .NET framework
- The utility builds cleanly with no warnings in touched files
