# .NET Version Upgrade Plan

## Overview

**Target**: Modernize the retained `x8086NetEmu` solution to `.NET 10` while retiring the legacy WinForms UI path in favor of the `Eto.Forms` applications.
**Scope**: Medium-sized mixed solution with 11 assessed projects, 10 retained for the upgrade path, and a split between already-modern `Eto.Forms` projects and legacy `.NET Framework 4.8.1` VB projects.

### Selected Strategy
**Hybrid** — Solution segmented into 5 groups with per-group strategies.
**Rationale**: The solution mixes already-modern `Eto.Forms` projects with classic VB projects that need SDK-style conversion and compatibility fixes. Most dependencies funnel through `x8086NetEmu.vbproj`, while the remaining risk is concentrated in the renderer and supporting legacy projects, so grouping by dependency path and risk reduces churn.

## Tasks

### 01-core-emulator: Upgrade the emulator core library

Upgrade `x8086NetEmu/x8086NetEmu.vbproj`, which is the main dependency root for the retained application path. This task covers converting the project to SDK style, retargeting it to `.NET 10`, and resolving the assessed binary, source, and behavioral compatibility issues so downstream projects can consume a modern core library.

**Done when**: `x8086NetEmu.vbproj` is SDK-style, targets `.NET 10`, its assessed compatibility issues are resolved, and dependent retained projects can reference it without target-framework conflicts.

---

### 02-supporting-tools: Upgrade supporting tooling and regression utilities

Upgrade `GenOpCodes/GenOpCodes.vbproj`, `RunTests/RunTests.vbproj`, and `RunTests2/RunTests2.csproj` as a low-risk tooling group after the core library is modernized. This keeps project conversion, target framework updates, and regression harness alignment together without entangling them with the UI migration path.

**Done when**: The tooling and regression projects target modern .NET, any required SDK-style conversions are complete, and the retained build/test workflow runs against the upgraded core library.

---

### 03-rendering-and-console: Upgrade the renderer and console runtime

Upgrade `x8086NetEmuRenderers/x8086NetEmuRenderers.vbproj` and `x8086NetEmuConsole/x8086NetEmuConsole.vbproj` after the core library is stable. This group isolates the remaining medium-risk desktop and graphics compatibility work, then revalidates the console path that depends on the renderer and core components.

**Done when**: The renderer and console projects are on modern .NET, renderer-specific compatibility issues are addressed, and the console path builds and runs against the upgraded dependencies.

---

### 04-eto-apps: Align the Eto.Forms application set with the upgraded libraries

Validate and align `Eto.Forms/x8086NetEmuEto/x8086NetEmuEto.csproj` together with the `Gtk`, `Mac`, and `Wpf` launcher projects after the shared VB dependencies are modernized. These projects already target modern .NET, so this task focuses on project-reference alignment, runtime behavior, and keeping the retained UI path healthy.

**Done when**: The `Eto.Forms` shared project and platform launchers build cleanly against the upgraded libraries, and their retained startup or smoke-test scenarios validate successfully.

---

### 05-winforms-retirement-and-solution-validation: Retire the WinForms path and validate the retained solution

Remove `x8086NetEmuWinForms/x8086NetEmuWinForms.vbproj` from the active modernization path and clean up any remaining solution/build assumptions that still treat it as a required deliverable. Then run final build and test validation across the retained projects so the upgraded solution reflects the `Eto.Forms` future state.

**Done when**: The WinForms project is excluded from the retained upgrade path as intended, the remaining projects target the planned modern .NET frameworks, and final retained-solution build and test validation succeeds.
