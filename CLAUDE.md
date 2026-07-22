# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

x8086NetEmu is a cycle-scheduled 8088/8086/80186 IBM PC (5150/5160) emulator. The CPU core and all
hardware emulation live in a VB.NET class library (`x8086NetEmu`); everything else (console app, GUI
frontends, test runners, code generators) consumes that library. No BIOS hacks are required — the
emulator boots real ROMs.

## Building and running

All projects target **net10.0**. There are two solutions:

- `x8086NetEmu.sln` — full solution: core library, console, tooling (GenOpCodes, RunTests, RunTests2),
  legacy renderers, and the Eto.Forms frontends.
- `Eto.Forms/Eto.Forms.sln` — just the cross-platform Eto GUI frontends.

```powershell
dotnet build x8086NetEmu.sln -c Debug          # or -c Release
dotnet run --project Eto.Forms/x8086NetEmuEto.Wpf   # run the Windows (WPF) GUI
dotnet run --project x8086NetEmuConsole             # run the console frontend
```

Build outputs go **directly to `Release/`**, not framework subfolders — the core `.vbproj` sets
`AppendTargetFrameworkToOutputPath=false` and `OutputPath=..\Release\`. Do not "fix" paths to expect
`Release/net10.0/`. The running emulator resolves ROMs, fonts, `settings.dat`, and disk images relative
to this folder via `X8086.BasePath` + `X8086.FixPath(...)`; ROMs are expected under `Release\roms\`.

## Running the CPU conformance tests

The emulator is validated against the TomHarte 8088 ProcessorTests (per-opcode JSON `.gz` files under
`Release/8088_ProcessorTests/v1/`). Two runners exist and do the same job:

- `RunTests2` (C#) — the current runner. `dotnet run --project RunTests2` (run from `Release/` so the
  relative `8088_ProcessorTests/v1` path resolves). It drives the CPU one opcode at a time via
  `cpu.PreExecute()` / `cpu.Execute_DEBUG()` / `cpu.PostExecute()` and diffs registers, flags, and RAM.
- `RunTests` (VB) — older equivalent.

`RunTests2/Program.cs` maintains explicit `skipOpCodes` / `ignoreFlags` lists for opcodes that are
unsupported or have known flag discrepancies (Group 2 shifts/rotates, Group 3 MUL/DIV/IDIV, AAM/AAD).
When touching opcode or flag logic, check whether an entry there should be removed.

## Architecture

**CPU core — `x8086NetEmu/x8086.vb`** (the `X8086` class, ~2200 lines). Holds registers, flags, memory,
the instruction decoder, and owns the chipset. The fetch/decode/execute loop runs on a background `Task`
started by `cpu.Run()`. `x8087.vb` is the FPU.

**Scheduler — `x8086NetEmu/Helpers/Misc/Scheduler.vb`.** Timing is cycle-driven: peripherals register
`SchTask`s and are clocked against `X8086.BASECLOCK` (4.77 MHz). Video adapters, PIT, etc. advance via
this scheduler rather than wall-clock timers.

**Chipset — `x8086NetEmu/Chipset/`.** Faithful part-numbered peripherals: `PIC8259` (interrupts),
`PIT8254` (timer), `DMA8237`, `PPI8255`, `RTC`. All are I/O-port devices built on `IOPortHandler` /
`IIOPortHandler`; the CPU dispatches `IN`/`OUT` to whichever handler claimed the port.

**Adapters — `x8086NetEmu/Adapters/`.** This is the extension model. `Adapter` (`Adapters/Adapter.vb`)
is `MustInherit` and inherits `IOPortHandler`. You register adapters with `cpu.Adapters.Add(...)`, which
calls `SetUpAdapter` to wire ports. Categories: `Video/`, `Audio/` (Adlib, SoundBlaster, Speaker),
`Disk/` (floppy + hard disk controllers, plus a `FileSystem/` FAT12/16 explorer), `Keyboard/`, `Serial/`.

**Host-agnostic rendering.** Video is split into two layers. The base classes (`VideoAdapter` →
`CGAAdapter`, `VGAAdapter`) live in the core library and are `MustInherit` — they emulate the video
hardware and character generation but leave actual pixel blitting abstract (`Render`, `AutoSize`,
`ResizeRenderControl`). Each host UI provides a concrete subclass that draws onto its native surface.
The Eto subclasses are in `Eto.Forms/x8086NetEmuEto/Renderers/` (e.g. `CGAEtoForms : CGAAdapter`);
legacy WinForms/Console/WebUI subclasses are in `x8086NetEmuRenderers/`. To add a rendering backend,
subclass the core video adapter — don't fork it.

## Frontends and the .NET-10 upgrade

The repo is mid-upgrade. Guidance that overrides appearances in the tree:

- **Eto.Forms is the frontend going forward; the WinForms version is discarded.** `Eto.Forms/` contains
  `x8086NetEmuEto` (shared UI) plus platform launchers `.Wpf`, `.Gtk`, `.Mac`. Do not invest in the old
  `x8086NetEmuWinForms` project. `MainForm.jeto.cs` shows the canonical wiring: construct `X8086`, then
  `Adapters.Add` a floppy controller, a CGA renderer, keyboard, mouse, and the audio adapters; load
  `settings.dat`; call `cpu.Run()`.
- **Eto emulator menu is keyboard-only:** shown solely via **Shift+Alt+Home**, never attached to the
  canvas right-click/context behavior, so canvas mouse input stays available to the emulated machine.
- **Eto mouse is hover-based and sticky:** while the cursor is over the emulator surface, hide the host
  cursor and forward all mouse events (including translating a host double-click into two full emulator
  click cycles). Once captured, input stays captured — moving outside the window does not release it;
  only Shift+Alt+Home does.

## Conventions

- The core library is VB.NET with `Option Strict Off`, `Option Explicit On`, `Option Infer On`. Frontends
  and the C# test runner are C#. Match the language and style of the file you are editing.
- Sound uses **ManagedBass** (cross-platform), with native `bass` libraries copied per-RID from
  `x8086NetEmu/Bass/`. A `Win32` compile constant (`Debug` config) historically gated the Windows-only
  audio path; when adding audio code keep it working with `Win32=False` for non-Windows builds.
- Opcode dispatch tables can be regenerated with the `GenOpCodes` tool (`ModuleMain.vb`) — hand-edit
  generated opcode code with care.
- During the upgrade workflow: run the ProcessorTests before opening a PR, and do not merge/rebase the
  working branch back into its source branch automatically — open the PR and let a human merge.
