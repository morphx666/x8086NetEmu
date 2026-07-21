# 05-winforms-retirement-and-solution-validation: Retire the WinForms path and validate the retained solution

Remove `x8086NetEmuWinForms/x8086NetEmuWinForms.vbproj` from the active modernization path and clean up any remaining solution/build assumptions that still treat it as a required deliverable. Then run final build and test validation across the retained projects so the upgraded solution reflects the `Eto.Forms` future state.

## Research Findings

### Projects Affected
- `x8086NetEmu.sln` — still includes `x8086NetEmuWinForms` in the solution and its build matrix, so retained-solution validation still tries to consider the discarded UI path.
- `x8086NetEmu/x8086NetEmu.vbproj` — still multi-targets `net10.0;net481`; the remaining `net481` bridge is only there for the discarded WinForms project.
- `x8086NetEmu/x8086NetEmu.vbproj.user` — still launches `Release\x8086NetEmuWinForms.exe`, which is a retained-path startup assumption that should be cleared or redirected.

### Scope and Decisions
- The WinForms project itself will be left on disk but removed from the retained solution/build path, matching the user preference to discard it without spending migration effort on it.
- No other retained project still targets `.NET Framework 4.8.1`; after WinForms removal the core emulator can collapse from multi-targeting back to a single `net10.0` target.
- `discover_test_projects` found no formal VS test projects in the retained path, so final validation will rely on full retained-solution build plus the existing regression harness executables and smoke runs.

### Validation Plan
- Remove the WinForms project from `x8086NetEmu.sln` and any direct retained-path startup assumptions.
- Retarget `x8086NetEmu.vbproj` from `net10.0;net481` to `net10.0` only and remove the conditional `net481` reference block.
- Run a full retained-solution build after the solution cleanup.
- Run retained regression/smoke validation from the `Release` layout, using the already-established ROM and output paths.

**Done when**: The WinForms project is excluded from the retained upgrade path as intended, the remaining projects target the planned modern .NET frameworks, and final retained-solution build and test validation succeeds.
