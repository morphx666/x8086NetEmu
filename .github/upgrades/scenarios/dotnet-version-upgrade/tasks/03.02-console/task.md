# 03.02-console: Upgrade the console runtime

# 03.02-console: Upgrade the console runtime

## Objective
Convert `x8086NetEmuConsole/x8086NetEmuConsole.vbproj` to SDK style and retarget it to modern .NET after the renderer library is upgraded, then validate that the console path builds against the upgraded dependencies.

## Scope
- `x8086NetEmuConsole/x8086NetEmuConsole.vbproj`
- `x8086NetEmuConsole/MainModule.vb` only if target-framework compatibility changes are required

## Done when
- `x8086NetEmuConsole.vbproj` is SDK-style
- The console project targets the planned modern .NET framework
- The console path builds cleanly against `x8086NetEmuRenderers` and `x8086NetEmu`

