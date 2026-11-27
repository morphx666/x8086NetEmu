# 03.01-renderers: Upgrade the renderer library

## Objective
Convert `x8086NetEmuRenderers/x8086NetEmuRenderers.vbproj` to SDK style, retarget it to a Windows-specific modern .NET target, and address the retained WinForms/System.Drawing compatibility issues needed for the non-WinForms future path.

## Scope
- `x8086NetEmuRenderers/x8086NetEmuRenderers.vbproj`
- Renderer source files and resources required to build on modern .NET
- WinForms and `System.Drawing` compatibility fixes that remain necessary for the retained renderer library

## Scope Inventory
- **Project affected**: `x8086NetEmuRenderers/x8086NetEmuRenderers.vbproj`
- **Distinct concerns**:
  - SDK-style conversion and retargeting to a Windows-specific modern TFM
  - `System.Drawing`/bitmap helper compatibility in `Helpers/DirectBitmap.vb` and `Helpers/Image2Ascii.vb`
  - WinForms control and designer compatibility in `WinForms/Controls/*`
  - Form-level compatibility in `WinForms/CGAWinForms.vb` and `WinForms/VGAWinForms.vb`
  - Secondary renderer surfaces in `CGAConsole.vb` and `WebUI.vb`
- **Change signals**:
  - 811 total assessed issues in this one project
  - Highest issue concentrations are `WinForms/VGAWinForms.vb` (294), `WinForms/CGAWinForms.vb` (245), `Helpers/Image2Ascii.vb` (68), `Helpers/DirectBitmap.vb` (64), and the custom WinForms controls (`RenderCtrl*.vb`)

## Research Findings
- The renderer project is still a classic VB WinForms library targeting `.NET Framework 4.8.1` with explicit references to `System.Drawing`, `System.Windows.Forms`, and `System.Web`.
- The assessment recommends a Windows-specific modern target (`net10.0-windows`) rather than a cross-platform TFM.
- The issue profile is not just structural conversion; it spans multiple independent code areas with different validation needs, so concern-level decomposition is safer than treating the whole renderer as one atomic change.

## Done when
- `x8086NetEmuRenderers.vbproj` is SDK-style
- The renderer targets the planned modern Windows-specific framework
- Required renderer compatibility fixes are applied
- The renderer builds cleanly against the upgraded `x8086NetEmu` core
