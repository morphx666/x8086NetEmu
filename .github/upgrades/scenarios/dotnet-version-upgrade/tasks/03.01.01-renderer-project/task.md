# 03.01.01-renderer-project: Convert and retarget the renderer project file

# 03.01.01-renderer-project: Convert and retarget the renderer project file

## Objective
Convert `x8086NetEmuRenderers/x8086NetEmuRenderers.vbproj` to SDK style and retarget it to the planned Windows-specific modern .NET framework while preserving references needed for the retained renderer library.

## Scope
- `x8086NetEmuRenderers/x8086NetEmuRenderers.vbproj`
- Project-level generated items only where the SDK-style conversion requires cleanup

## Done when
- The project file is SDK-style
- The project targets the planned modern Windows-specific framework
- The converted project restores/builds far enough to expose code-level compatibility work cleanly

