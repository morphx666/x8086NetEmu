# 04-eto-apps: Align the Eto.Forms application set with the upgraded libraries

Validate and align `Eto.Forms/x8086NetEmuEto/x8086NetEmuEto.csproj` together with the `Gtk`, `Mac`, and `Wpf` launcher projects after the shared VB dependencies are modernized. These projects already target modern .NET, so this task focuses on project-reference alignment, runtime behavior, and keeping the retained UI path healthy.

**Done when**: The `Eto.Forms` shared project and platform launchers build cleanly against the upgraded libraries, and their retained startup or smoke-test scenarios validate successfully.
