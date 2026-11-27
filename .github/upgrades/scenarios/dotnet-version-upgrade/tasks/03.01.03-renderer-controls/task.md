# 03.01.03-renderer-controls: Modernize WinForms renderer controls

# 03.01.03-renderer-controls: Modernize WinForms renderer controls

## Objective
Resolve the retained WinForms compatibility issues in `WinForms/Controls/RenderCtrl*` so the renderer control layer builds under the modern Windows-specific target.

## Scope
- `x8086NetEmuRenderers/WinForms/Controls/RenderCtrl.vb`
- `x8086NetEmuRenderers/WinForms/Controls/RenderCtrl.Designer.vb`
- `x8086NetEmuRenderers/WinForms/Controls/RenderCtrlGDI.vb`
- `x8086NetEmuRenderers/WinForms/Controls/RenderCtrlGDI.Designer.vb`
- Related `.resx` files if designer/resource fixes are needed

## Done when
- The retained renderer controls are compatible with the modern target
- Control-layer files build cleanly under the modern renderer project

