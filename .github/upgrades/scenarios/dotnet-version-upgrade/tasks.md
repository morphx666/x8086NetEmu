# .NET Version Upgrade Progress

## Overview

Upgrade the retained `x8086NetEmu` projects to `.NET 10` using a hybrid approach that modernizes the legacy VB dependency chain first, then aligns the already-modern `Eto.Forms` applications. The WinForms UI path is intentionally excluded from the target end state.

**Progress**: 1/5 tasks complete <progress value="20" max="100"></progress> 20%

## Tasks

- ✅ 01-core-emulator: Upgrade the emulator core library ([Content](tasks/01-core-emulator/task.md), [Progress](tasks/01-core-emulator/progress-details.md))
- 🔲 02-supporting-tools: Upgrade supporting tooling and regression utilities
- 🔲 03-rendering-and-console: Upgrade the renderer and console runtime
- 🔲 04-eto-apps: Align the Eto.Forms application set with the upgraded libraries
- 🔲 05-winforms-retirement-and-solution-validation: Retire the WinForms path and validate the retained solution
