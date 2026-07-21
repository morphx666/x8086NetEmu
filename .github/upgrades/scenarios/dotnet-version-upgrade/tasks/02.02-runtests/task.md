# 02.02-runtests: Upgrade the legacy RunTests harness

## Objective
Convert `RunTests/RunTests.vbproj` to SDK style, retarget it to modern .NET, and replace the remaining `My.Application.Info.DirectoryPath` usage so the legacy regression harness builds against the upgraded emulator core.

## Scope
- `RunTests/RunTests.vbproj`
- `RunTests/ModuleMain.vb`
- Generated `My Project` items only if the SDK-style conversion requires trimming unused VB application/settings artifacts

## Research Findings
- `RunTests` is a small VB console harness that references `x8086NetEmu.vbproj`.
- The assessment found one concrete code incompatibility in `ModuleMain.vb`: the test-data directory is built from `My.Application.Info.DirectoryPath`.
- The rest of the expected work is project-format modernization, target framework retargeting, and validating the harness against the upgraded core library.

## Done when
- `RunTests.vbproj` is SDK-style
- `RunTests.vbproj` targets the planned modern .NET framework or bridge target as needed
- The test-data path lookup no longer depends on `My.Application.Info.DirectoryPath`
- The harness builds cleanly against `x8086NetEmu`
