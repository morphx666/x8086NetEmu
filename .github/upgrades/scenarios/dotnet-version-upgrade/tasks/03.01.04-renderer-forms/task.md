# 03.01.04-renderer-forms: Modernize renderer forms and secondary surfaces

# 03.01.04-renderer-forms: Modernize renderer forms and secondary surfaces

## Objective
Resolve the remaining form-level and secondary renderer compatibility issues in `CGAConsole.vb`, `WebUI.vb`, `WinForms/CGAWinForms.vb`, and `WinForms/VGAWinForms.vb`, then validate the full renderer project.

## Scope
- `x8086NetEmuRenderers/CGAConsole.vb`
- `x8086NetEmuRenderers/WebUI.vb`
- `x8086NetEmuRenderers/WinForms/CGAWinForms.vb`
- `x8086NetEmuRenderers/WinForms/VGAWinForms.vb`
- Any final cross-file adjustments needed for renderer project validation

## Done when
- Form-level and secondary renderer compatibility issues needed for build success are addressed
- The full renderer project builds cleanly against the upgraded core

