# 04-eto-apps: Align the Eto.Forms application set with the upgraded libraries

Validate and align `Eto.Forms/x8086NetEmuEto/x8086NetEmuEto.csproj` together with the `Gtk`, `Mac`, and `Wpf` launcher projects after the shared VB dependencies are modernized. These projects already target modern .NET, so this task focuses on project-reference alignment, runtime behavior, and keeping the retained UI path healthy.

## Research Findings

### Projects Affected
- `Eto.Forms/x8086NetEmuEto/x8086NetEmuEto.csproj` — shared Eto UI project; currently builds on `net10.0`, but `Renderers/VideoChar.cs` still emits `CS0659`/`CS0661` because equality members do not include `GetHashCode()`.
- `Eto.Forms/x8086NetEmuEto.Wpf/x8086NetEmuEto.Wpf.csproj` — retained Windows launcher; builds on `net10.0-windows7.0`, but restore emits `NU1701` because the transitive `Extended.Wpf.Toolkit 3.6.0` package selected by `Eto.Platform.Wpf 2.11.0` is framework-only.
- `Eto.Forms/x8086NetEmuEto.Mac/x8086NetEmuEto.Mac.csproj` — macOS launcher; builds on `net10.0`, but when built on Windows with both runtime identifiers it emits the Eto packaging warning `Can only create universal binary on macOS`.
- `Eto.Forms/x8086NetEmuEto.Gtk/x8086NetEmuEto.Gtk.csproj` — GTK launcher; no project-file issues were found, but it inherits warnings from the shared Eto project.

### Files to Modify
- `Eto.Forms/x8086NetEmuEto/Renderers/VideoChar.cs` — add a `GetHashCode()` implementation consistent with the existing equality operators.
- `Eto.Forms/x8086NetEmuEto.Wpf/x8086NetEmuEto.Wpf.csproj` — add an explicit modern `Extended.Wpf.Toolkit` package reference to override the incompatible transitive version.
- `Eto.Forms/x8086NetEmuEto.Mac/x8086NetEmuEto.Mac.csproj` — conditionally limit non-macOS builds to a single RID so Windows validation does not try to create a universal macOS bundle.

### Package and Build Findings
- `Eto.Platform.Wpf`, `Eto.Platform.Mac64`, and `Eto.Forms` are already at the latest supported `2.11.0` versions for the current target frameworks.
- `Extended.Wpf.Toolkit` has a newer supported version `5.1.2` for `net10.0-windows7.0`; this is the candidate fix for the WPF restore warning.
- The Eto Mac package target creates unified bundles whenever multiple macOS runtime identifiers are present and warns on non-macOS hosts, so the cleanest repo-side mitigation is to keep dual-RID publishing only on macOS.

### Validation Plan
- Rebuild the shared Eto project and each launcher after warning cleanup.
- Smoke-test the retained Windows Eto launcher path by starting the WPF launcher briefly on the local Windows environment.
- Keep Gtk and Mac validation at build level in this environment because those launchers are not runnable on the current Windows host.

**Done when**: The `Eto.Forms` shared project and platform launchers build cleanly against the upgraded libraries, and their retained startup or smoke-test scenarios validate successfully.
