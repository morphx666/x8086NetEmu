# 05-winforms-retirement-and-solution-validation: Retire the WinForms path and validate the retained solution

Remove `x8086NetEmuWinForms/x8086NetEmuWinForms.vbproj` from the active modernization path and clean up any remaining solution/build assumptions that still treat it as a required deliverable. Then run final build and test validation across the retained projects so the upgraded solution reflects the `Eto.Forms` future state.

**Done when**: The WinForms project is excluded from the retained upgrade path as intended, the remaining projects target the planned modern .NET frameworks, and final retained-solution build and test validation succeeds.
