## Files Modified
- .github/upgrades/scenarios/dotnet-version-upgrade/tasks/04-eto-apps/task.md
- Eto.Forms/x8086NetEmuEto/Renderers/VideoChar.cs
- Eto.Forms/x8086NetEmuEto.Wpf/x8086NetEmuEto.Wpf.csproj
- Eto.Forms/x8086NetEmuEto.Mac/x8086NetEmuEto.Mac.csproj

## Build Result
- Errors: 0 in the Eto.Forms projects touched by this task
- Warnings: 0 in the Eto.Forms projects touched by this task
- Projects built:
  - Eto.Forms/x8086NetEmuEto/x8086NetEmuEto.csproj
  - Eto.Forms/x8086NetEmuEto.Wpf/x8086NetEmuEto.Wpf.csproj
  - Eto.Forms/x8086NetEmuEto.Gtk/x8086NetEmuEto.Gtk.csproj
  - Eto.Forms/x8086NetEmuEto.Mac/x8086NetEmuEto.Mac.csproj
- Additional validation:
  - `run_build` on the full solution still fails outside this task's scope because the solution retains the excluded WinForms project and the core project still participates in a temporary `net481` bridge. Those retained-solution cleanup items belong to the next task.

## Test Result
- Tests run: 0
- Passed: 0
- Failed: 0
- Notes: `discover_test_projects` returned no test projects for the Eto.Forms app set, so validation for this task was build plus launcher smoke-test based.

## Changes Summary
- Enriched the Eto alignment task record with concrete warning sources, affected files, package findings, and validation strategy.
- Implemented `VideoChar.GetHashCode()` so the shared Eto project no longer emits `CS0659`/`CS0661`.
- Added an explicit `Extended.Wpf.Toolkit` `5.1.2` package reference in the WPF launcher to override the incompatible transitive `3.6.0` dependency and remove `NU1701`.
- Conditioned the Mac launcher runtime identifiers so Windows-hosted validation builds only a single macOS RID instead of attempting a universal bundle.
- Added `Icon.icns` as content so the Mac packaging targets copy the icon into the output bundle without warnings.

## Runtime / Smoke Validation
- The retained WPF launcher was started from `Release\Eto.Forms\Debug` with `dotnet .\x8086NetEmuEto.Wpf.dll`.
- The process stayed running with no immediate console faults until it was manually interrupted, which is consistent with a GUI app entering its event loop successfully.
- GTK and Mac launchers were validated at build level only because the current host environment is Windows.

## Issues Encountered
- `Eto.Platform.Wpf 2.11.0` still resolves `Extended.Wpf.Toolkit 3.6.0` transitively, which triggers `NU1701` on `net10.0-windows7.0`; resolved by pinning `Extended.Wpf.Toolkit` to supported version `5.1.2` in the launcher project.
- The Eto Mac build targets emit a non-macOS universal-binary warning when multiple runtime identifiers are present; resolved by keeping dual-RID publishing only on macOS hosts.
- Full-solution validation currently still reports known retained-solution issues tied to the excluded WinForms path and the temporary core bridge; deferred to `05-winforms-retirement-and-solution-validation` per the scenario plan.
