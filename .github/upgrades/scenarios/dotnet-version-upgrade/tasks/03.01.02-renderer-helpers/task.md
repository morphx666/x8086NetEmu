# 03.01.02-renderer-helpers: Modernize renderer helper types

# 03.01.02-renderer-helpers: Modernize renderer helper types

## Objective
Resolve the helper-layer compatibility issues in `Helpers/DirectBitmap.vb`, `Helpers/Image2Ascii.vb`, and related helper files so the modernized renderer can continue using its retained Windows drawing path.

## Scope
- `x8086NetEmuRenderers/Helpers/DirectBitmap.vb`
- `x8086NetEmuRenderers/Helpers/Image2Ascii.vb`
- Any closely related helper files needed to restore build compatibility

## Done when
- Helper-layer `System.Drawing` compatibility issues required for build success are addressed
- The helper files build cleanly under the modern renderer project

