# 03-rendering-and-console: Upgrade the renderer and console runtime

Upgrade `x8086NetEmuRenderers/x8086NetEmuRenderers.vbproj` and `x8086NetEmuConsole/x8086NetEmuConsole.vbproj` after the core library is stable. This group isolates the remaining medium-risk desktop and graphics compatibility work, then revalidates the console path that depends on the renderer and core components.

**Done when**: The renderer and console projects are on modern .NET, renderer-specific compatibility issues are addressed, and the console path builds and runs against the upgraded dependencies.
