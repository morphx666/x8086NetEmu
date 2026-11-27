# 02-supporting-tools: Upgrade supporting tooling and regression utilities

Upgrade `GenOpCodes/GenOpCodes.vbproj`, `RunTests/RunTests.vbproj`, and `RunTests2/RunTests2.csproj` as a low-risk tooling group after the core library is modernized. This keeps project conversion, target framework updates, and regression harness alignment together without entangling them with the UI migration path.

## Scope Inventory
- **Projects affected**: `GenOpCodes/GenOpCodes.vbproj`, `RunTests/RunTests.vbproj`, `RunTests2/RunTests2.csproj`
- **Distinct concerns**:
  - Convert the two legacy VB console tools to SDK-style projects
  - Retarget all tooling to modern .NET while preserving the temporary bridge to the upgraded core library
  - Replace the remaining `My.Application.Info.DirectoryPath` usage in the legacy regression harness
  - Validate that both regression entry points still build against the modernized `x8086NetEmu` library
- **Change signals**:
  - `GenOpCodes` had only project-format and target-framework issues and is now modernized
  - `RunTests` had project-format and target-framework issues plus one concrete API pattern (`My.Application.Info.DirectoryPath`) and is now modernized
  - `RunTests2` is already SDK-style and only needs target-framework alignment

## Research Findings
- `GenOpCodes` is a small standalone console tool with no project references and no assessed API incompatibilities.
- `RunTests` references `x8086NetEmu.vbproj` and its only assessed code incompatibility was the test-directory lookup in `ModuleMain.vb`.
- `RunTests2` is already SDK-style, references `x8086NetEmu.vbproj`, and still targets `net481`; it can be retargeted now that the core library bridge is in place.
- The workflow collapsed the temporary child subtasks back into the parent task state after completion, so the remaining `RunTests2` retargeting is being finalized directly under this parent task.

**Done when**: The tooling and regression projects target modern .NET, any required SDK-style conversions are complete, and the retained build/test workflow runs against the upgraded core library.
