# x8086NetEmu 
A VB.NET implementation of an almost working 8086 emulator.

[![Build Status](https://xfx.visualstudio.com/x8086NetEmu/_apis/build/status/morphx666.x8086NetEmu?branchName=master)](https://xfx.visualstudio.com/x8086NetEmu/_build/latest?definitionId=8&branchName=master)

![x8086NetEmu Showcase](https://xfx.net/stackoverflow/x8086netEmu/x8086NetEmu_01.gif)

Although it still has some bugs, it is a fairly stable and capable 8088/86/186 emulator:

- Full 8086 architecture emulation: CPU, Memory, Flags, Registers and Stack
- Peripherals: PIC/8259, PIT/8254, DMA/8237 and PPI/8255
- Mostly working Adapters: CGA, Speaker and Keyboard
- Partially working Adapters: VGA, Adlib and Mouse
- Integrated Debugger and Console
- No BIOS hacks required
- WinForms and Console samples included
- Cross-platform support through Mono (the emulator has been tested under Windows, [MacOS, Linux and RaspberryPi](https://whenimbored.xfx.net/2013/10/x8086netemu-linux-mac-os-x-raspberry-pi/))
- Support for both Floppy and Hard Disk images
- Hard disk and floppy images inspector / Disk Explorer (FAT12 and FAT16 support only)
- Support to drag & drop files and folders from the Disk Explorer to the host and viceversa
- Support to copy/paste text to/from the emulator and the host

![Integrated Debugger](http://whenimbored.xfx.net/wp-content/uploads/2018/01/debugger.png)

Development is currently stalled due to a breaking bug (or bugs?) which prevent the emulator from working properly.
The bug can be reproduced by booting into DOS 6.x and running EDIT, QBASIC, DEFRAG or MEMMAKER.
Quite probably, this is the same bug that also prevents it from running Windows 1.01 (although Windows 2.03 *almost* works).

Portions of the code in the emulator were adapted or inspired from '[fake86](https://github.com/rubbermallet/fake86)' (CGA emulation), '[PCE - PC Emulator](http://www.hampa.ch/pce/)' ([Group 2](http://www.mlsite.net/8086/), DIV, IDIV, MUL and IMUL opcodes emulation and flags management) and '[retro](http://jorisvr.nl/article/retro)' (Scheduler, chipset and keyboard handling).

Precompiled binaries can now be downloaded from the [releases](https://github.com/morphx666/x8086NetEmu/releases) section.

### Compiling for non-Windows platforms

The speaker emulation uses [NAudio](https://github.com/naudio/NAudio), which only works under Windows.
So in order to compile a version of x8086 that works under non-Windows platforms, the Win32 custom constant in the project properties of all the projects in the solution must be set to `False`.

If the aforementioned bug or bugs can be resolved, I will switch the sound backend support to the cross-platform library [BASS](http://www.un4seen.com/).

### Experimental Web UI

![Experimental Web UI](https://xfx.net/stackoverflow/x8086netEmu/x8086_WebUI_01.png)

Since [commit 248](https://github.com/morphx666/x8086NetEmu/commit/c08b69b7c6ffbe165a036b811ff8e2b71e529854) the emulator can be viewed and controlled through a browser by initializing one of the WinForms video adapters with the `enableWebUI` parameter set to `true`.

`cpu.Adapters.Add(New CGAWinForms(cpu, videoPort, ,, True))`

This will create a web server at http://localhost:8086 which uses a simple script to render the emulator's display and capture key events, which will be sent back to the emulator for processing.
Mouse support is not currently available.
