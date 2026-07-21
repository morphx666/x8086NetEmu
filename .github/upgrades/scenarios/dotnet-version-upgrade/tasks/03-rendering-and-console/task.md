# 03-rendering-and-console: Upgrade the renderer and console runtime

Upgrade `x8086NetEmuRenderers/x8086NetEmuRenderers.vbproj` and `x8086NetEmuConsole/x8086NetEmuConsole.vbproj` after the core library is stable. This group isolates the remaining medium-risk desktop and graphics compatibility work, then revalidates the console path that depends on the renderer and core components.

## Scope Inventory
- **Projects affected**: `x8086NetEmuRenderers/x8086NetEmuRenderers.vbproj`, `x8086NetEmuConsole/x8086NetEmuConsole.vbproj`
- **Distinct concerns**:
  - Convert both remaining legacy VB projects to SDK style
  - Retarget the renderer to a Windows-specific modern TFM because it still depends heavily on WinForms and `System.Drawing`
  - Retarget the console app only after the renderer is modernized because it references the renderer and the core library
- **Change signals**:
  - `x8086NetEmuRenderers` has 811 assessed issues dominated by WinForms and GDI+/System.Drawing usage, making it the highest-risk retained project group so far
  - `x8086NetEmuConsole` has only structural project-format and target-framework issues but depends on the renderer and core library

## Research Findings
- `x8086NetEmuRenderers` is a legacy Windows-centric VB library with WinForms controls, `.resx` resources, `System.Drawing`, and a project reference to `x8086NetEmu`.
- `x8086NetEmuConsole` is comparatively small, but it references both `x8086NetEmuRenderers` and `x8086NetEmu`, so it should follow the renderer in dependency order.
- These projects require different validation focus and have a clear dependency boundary, so project-level decomposition reduces failure blast radius and keeps the console upgrade from masking renderer-specific problems.

**Done when**: The renderer and console projects are on modern .NET, renderer-specific compatibility issues are addressed, and the console path builds and runs against the upgraded dependencies.
