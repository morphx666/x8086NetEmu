# x8086NetEmu 
A VB.NET implementation of an almost working 8086 emulator.

![x8086NetEmu Showcase](https://xfx.net/stackoverflow/x8086netEmu/x8086NetEmu_01.gif)

Although it still has some bugs, it is a fairly stable and capable 8088/86/186 emulator:

- Full 8086 architecture emulation: CPU, Memory, Flags, Registers, and Stack, validated against the [8088 ProcessorTests](https://github.com/TomHarte/ProcessorTests/tree/main/8088)
- Cycle-scheduled timing: the CPU clock is configurable (4.77 MHz and up) while the peripherals keep running at their real rates
- Peripherals: PIC/8259, PIT/8254, DMA/8237 and PPI/8255
- Video: CGA, and a register-level VGA driven by a real VGA BIOS (standard VGA modes; SVGA modes are not emulated)
- Sound: PC Speaker, Adlib (an OPL2/YM3812 FM synthesizer, including rhythm mode) and Sound Blaster 2.0 (DMA playback through the emulated 8237)
- Input: Keyboard and a Microsoft serial mouse on COM1 (an emulated 8250 UART)
- No BIOS hacks are required
- Cross-platform .NET 10 frontends built with [Eto.Forms](https://github.com/picoe/Eto) for Windows (WPF), Linux (GTK) and macOS
- Legacy WinForms and Console frontends are still included
- Support for both Floppy and Hard Disk images
- Hard disk and floppy images inspector / Disk Explorer (FAT12, FAT16, and FAT16B/BIGDOS support only)
- Support to drag & drop files to/from the Disk Explorer and the host
- Support to copy/paste text to/from the emulator and the host
- The Emulator's menu is opened with Shift+Alt+Home in the Eto.Forms frontends (RCtrl+Home in the WinForms frontend). No menu is available in the Console version: to change its settings, edit the `settings.dat` file using any text editor
- The mouse is captured while the pointer is over the emulator; it's released with Shift+Alt+Home or by pushing the pointer firmly past any corner of the emulator's display
- Text-mode characters come from the machine's own ROMs: the CGA font is built from the BIOS character set, and the VGA uses the font its video BIOS loads into video memory
- Integrated Debugger and Console

![Integrated Debugger](http://whenimbored.xfx.net/wp-content/uploads/2018/01/debugger.png)

~~Development is currently stalled due to a breaking bug (or bugs?) which prevent the emulator from working correctly.
The bug can be reproduced by booting into DOS 6.x and running EDIT, QBASIC, DEFRAG or MEMMAKER.
Quite probably, this is the same bug that also prevents it from running Windows 1.01 (although Windows 2.03 *almost* works).~~

Thanks to the amazing work by [@TomHarte](https://github.com/TomHarte/ProcessorTests/tree/main/8088) I was able to finally find the bug that was affecting so many programs!
So it happens that the `TEST` opcode (0x84) was using the incorrect register when in indirect mode. That was it... fixing that stupid mistake has solved all the bugs mentioned above.

Portions of the code in the emulator were adapted or inspired from '[PCE - PC Emulator](http://www.hampa.ch/pce/)' ([Group 2](http://www.mlsite.net/8086/), DIV, IDIV, MUL and IMUL opcodes emulation and flags management) and '[retro](http://jorisvr.nl/article/retro)' (Scheduler, chipset and keyboard handling).

Precompiled binaries can now be downloaded from the [releases](https://github.com/morphx666/x8086NetEmu/releases) section.

### Frontends

The project has moved to [Eto.Forms](https://github.com/picoe/Eto), which provides native frontends for each platform from a single code base:

| Platform | Project |
|---|---|
| Windows | `Eto.Forms/x8086NetEmuEto.Wpf` |
| Linux | `Eto.Forms/x8086NetEmuEto.Gtk` |
| macOS | `Eto.Forms/x8086NetEmuEto.Mac` |

The original WinForms frontend (`x8086NetEmuWinForms`, Windows only) and the Console frontend (`x8086NetEmuConsole`) are still part of the solution, but new development happens on the Eto.Forms frontends.

### Building and running

Everything targets .NET 10. Build outputs, ROMs, disk images and `settings.dat` all live in the `Release` folder.

```
dotnet build x8086NetEmu.sln -c Release
dotnet run --project Eto.Forms/x8086NetEmuEto.Wpf    # Windows
dotnet run --project Eto.Forms/x8086NetEmuEto.Gtk    # Linux
dotnet run --project Eto.Forms/x8086NetEmuEto.Mac    # macOS
```

Sound uses the cross-platform [BASS](http://www.un4seen.com/) library (through ManagedBass); the native libraries for Windows, Linux and macOS are included under `x8086NetEmu/Bass`.

### Experimental Web UI

![Experimental Web UI](https://xfx.net/stackoverflow/x8086netEmu/x8086_WebUI_01.png)

Since [commit 248](https://github.com/morphx666/x8086NetEmu/commit/c08b69b7c6ffbe165a036b811ff8e2b71e529854) the emulator can be viewed and controlled through a browser by initializing the WinForms CGA video adapter with the `enableWebUI` parameter set to `true` (this is only available in the WinForms frontend).

`cpu.Adapters.Add(New CGAWinForms(cpu, videoPort, , True))`

This will create a web server at http://localhost:8086 which uses a simple script to render the emulator's display and capture key events, which will be sent back to the emulator for processing.
Mouse support is not currently available.
