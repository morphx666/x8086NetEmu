# x8086NetEmu
A VB.NET implementation of an almost working 8086 emulator.

![x8086NetEmu running PacMan under Linux](https://whenimbored.xfx.net/wp-content/uploads/2012/10/x8086_emu_game2.png)

Although it still has some bugs, it is a fairly stable and capable 8088/86/186 emulator:

- Full 8086 architecture emulation: CPU, Memory, Flags, Registers and Stack
- Peripherals: PIC/8259, PIT/8254, DMA/8237 and PPI/8255
- Adapters: CGA, Speaker, Keyboard and Mouse (mouse emulation is still quite buggy so it's disabled by default)
- Integrated Debugger and Console
- Support for both Floppy and Hard Disk images
- No BIOS hacks required
- WinForms and Console samples included
- Cross-platform support through Mono (the emulator has been tested under Windows, [MacOS, Linux and RaspberryPi/Raspbian](https://whenimbored.xfx.net/2013/10/x8086netemu-linux-mac-os-x-raspberry-pi/))

![Integrated Debugger](http://whenimbored.xfx.net/wp-content/uploads/2012/09/debug.png)

Development is currently stalled due to a breaking bug (or bugs) which prevent the emulator from working properly.
The bug can be reproduced by booting into DOS 6.x and running EDIT, QBASIC, DEFRAG or MEMMAKER.
Quite probably, this is the same bug that also prevents it from running Windows 1.01

Portions of the code in the emulator were adapted or inspired from '[fake86](https://github.com/rubbermallet/fake86)' (CGA emulation), '[PCE - PC Emulator](http://www.hampa.ch/pce/)' ([Group 2](http://www.mlsite.net/8086/), DIV, IDIV, MUL and IMUL opcodes emulation and flags management) and '[retro](http://jorisvr.nl/article/retro)' (Scheduler, chipset and keyboard handling).

Precompiled binaries, including various boot disks, are available at https://whenimbored.xfx.net/2012/10/x8086netemu-an-8086-emulator-in-vb-net/
