============================================================================
      Super PC/Turbo XT BIOS for Intel 8088 or NEC "V20" Motherboards
Additions by Ya`akov Miles (1987) and Jon Petrosky <Plasma> (2008,2011-2012)
                          http://www.phatcode.net/
----------------------------------------------------------------------------
                          Version 2.5 - May 2 2012
============================================================================


About
=====
This is a modification of the widely-distributed "(c) Anonymous Generic
Turbo XT Anonymous" BIOS, which is actually a Taiwanese BIOS that was
reverse-engineered by Ya`akov Miles in 1987.

Back in 2008 I put together an XT system and wanted a BIOS that supported
booting from a hard drive. The Generic XT BIOS did not support this, but
since source code was provided it was easy to add this feature. While I was
at it, I fixed some bugs I found, added more features, and cleaned up the
code. Initially I only modified this BIOS for my computer, but I decided I
might as well release my new version in case it proves useful for someone
else.

In 2011 I was informed that this BIOS did not work correctly with the
original IBM PC (5150). I made some additional changes and now the 5150 is
supported as well as the 5160 (XT) and just about all PC/XT clones.

You do not need to have a turbo motherboard to use this BIOS, but if you do,
then you can use the "CTRL ALT -" key combination to toggle the computer
speed between fast and slow. When the speed is toggled, the PC speaker will
sound a high/low pitched blip to indicate the system speed selected.


Changes
=======
Version 2.5 Changes:

Added:
  * Option to clear user-defined memory region in upper memory area (UMA)
      for systems with non-EMS UMBs. These UMBs should be cleared before
      use or parity errors may occur. Define CLEAR_UMA and set the region
      with UMA_START and UMA_END. Because of ROM space limitations,
      TEST_VIDEO must be disabled if using CLEAR_UMA.
  * Option to display ROM BASIC boot prompt during warm boot (define
      WARM_BOOT_BASIC).

Version 2.4 Changes:

Added:
  * Improved support for 101-key enhanced keyboards (define ENHANCED_KEYB to
      enable)
  * CPU test now optional (define TEST_CPU). Must be disabled if
      ENHANCED_KEYB is enabled due to ROM space limitations.
  * Video memory test now optional (define TEST_VIDEO). Applies only to
      Mono/Herc/CGA cards; video memory is never tested on EGA/VGA cards.

Changed:
  * Int 16h extended keyboard functions now fully implemented
      (ah=00h/01h/02h/05h/10h/11h/12h). Note that ENHANCED_KEYB does not need
      to be enabled to use these functions.
  * KEYB_SHIFT_FIX removed; use ENHANCED_KEYB instead.
  * NO_MEM_CHECK now faster; only zeroes out memory and does not blank check.
  * Removed Int 15h hooks for future expansion BIOS (not used)

Version 2.3 Changes:

Changed:
  * Int 16h enhanced keyboard functions (ah=10h/11h/12h) now mapped to
      standard functions (ah=00h/01h/02h) for programs that expect enhanced
      keyboard support.

Version 2.2 Changes:

Added:
  * Make batch file generates proper 32K ROMs for IBM 5155 and 5160

Bug Fixed/Changed:
  * FAST_MEM_CHECK option now clears memory after testing. This fixes problems
      with programs unable to find free interrupt vectors. However the "fast"
      memory check is now slower. For the fastest startup you can now disable
      the memory check with the NO_MEM_CHECK option.

Version 2.1 Changes:
 
Added:
  * Optional define for IBM PC 5150 support (config switches on motherboard
      are read differently)
  * Original IBM PC 5150 Int 2 (NMI) entry point now supported for better IBM
      PC compatibility
  * Optional define to disable turbo support completely (for non-turbo 4.77
      MHz systems)
  * Int 15h hooks are called for future expansion BIOS to display drives and
      boot menu if present
  * Optional define to set boot delay length

Changed:
  * Hard drive boot sector load is now only attempted 2 times rather than 4
  * Boot delay now based on system timer rather than fixed loops, useful for
      very fast systems
  * Pressing any key during boot delay message will end delay and start booting

Bug Fixes:
  * Boots to BASIC if no floppy or hard drive controller (previously would
      hang)
  * Screen cleared after error if user chooses to continue booting

Version 2.0 Changes:

Code:
  * Changed from MASM 4.x to TASM IDEAL mode
  * Cleaned up source code: added procs, more descriptive labels
    (...still some spaghetti due to necessary hard-coded entry points)

Bug Fixes:
  * Warm boot flag restored after external ROM initialization code (fixes
      CTRL+ALT+DEL warm boot)
  * Equipment Flag in BIOS Data Area now set correctly
  * Fixed cursor shape when using Hercules card (was in the middle of the
      character)

Added:
  * Optional define to always boot up in turbo mode (TURBO_BOOT)
  * Optional define for fast memory check (FAST_MEM_CHECK); uses one test
      pattern only
  * Optional define for 101-key keyboard shift+nav keys work-around
      (KEYB_SHIFT_FIX)
  * BIOS is now EGA/VGA aware; will only test video memory on Mono/Herc/CGA
      cards
  * Nicer boot screen with color support for CGA/EGA/VGA
  * Processor and Math Coprocessor detection
  * Memory test displays count every 8K; speeds up check on fast systems with
      slow video cards
  * User has option to boot ROM BASIC even if bootable disk present
  * Supports booting from hard drive (if external controller ROM present)
  * Toggling turbo on/off sounds high/low-pitched beep rather than changing
      cursor shape


Notes
=====

Error Codes
-----------
The BIOS may give a "System Error" code at bootup. This error code is a
combination of the following codes:

01h - Bad system BIOS checksum
02h - Bad RAM in main system memory
04h - Bad RAM in video card
10h - Bad RAM in vector area (this also in main system memory)
20h - Bad expansion ROM checksum

Note that the codes are in hexadecimal. The "System Error" code given by the
BIOS is a sum of the above codes. For example, if the code 26 is given, the
individual errors are 02h, 04h, and 20h.

Hard Drives
-----------
The BIOS supports booting from hard drives through the INT 13h interface,
but it _does not_ interface with the drive itself. This means that an
external hard drive BIOS must be present, either on the drive controller
or on the motherboard.

Floppy Drives
-------------
High-density floppy disks (3.5" 1.44MB and 5.25" 1.2MB) are not supported due
to lack of ROM space. You will need to use an external floppy controller BIOS
for this. Another option is to run DR-DOS, which loads software support for
high-density drives (this still requires a high-density controller, but no ROM
required). There is also a DOS TSR called "2M-XBIOS" which adds this support to
any DOS, but you cannot boot from a high-density disk.

Cassette Support
----------------
Cassette functions are not supported, also due to lack of ROM space. This only
affects IBM PC 5150s or exact clones since XTs do not have a cassette port.

Special Thanks
--------------
I would like to thank Michael Mruzek for donating an IBM 5150 motherboard so
I could implement original PC support. I would also like to thank Greg Saling
for identifying several bugs and helping me improve the BIOS compatibility.


Building and Installing the BIOS
================================

Source Code
-----------
The source code for the BIOS is provided in the file PCXTBIOS.ASM. It has
been formatted for a 100-column page with 8-space tabs. The BIOS will
assemble with any version of TASM. (It will not assemble with LZASM; I'm
not quite sure why...)

Once assembled and linked, the actual BIOS image is the last 8K of the
EXE produced, with the exception of the final checksum byte which needs
to be calculated and added to the end. (The checksum byte makes the sum
of all the bytes in the image = 0 mod 256.)

Options
-------
There are 15 optional defines in the BIOS source code. To enable/disable
them, simply comment or uncomment the definition in the code and then
rebuild the BIOS.

IBM_PC:
    Define only if using with original IBM PC (5150) or exact clone. This
    will read the 5150 config switches correctly and set the BIOS computer
    type to FFh (PC) rather than FEh (XT). You should also disable the
    TURBO_ENABLED, TURBO_BOOT, and SLOW_FLOPPY definitions if using with an
    original PC.

TURBO_ENABLED:
    Define to enable "turbo" support and the CTRL ALT - hotkey for switching
    from fast/slow speeds. If you have a non-turbo system (4.77 MHz) you
    can disable this definition.

TURBO_BOOT:
    If defined, system speed is switched to "turbo" at bootup. This is done
    after hardware detection, but before external ROM initialization and
    memory check. Has no effect on non-turbo systems.

SLOW_FLOPPY:
    If defined, will always run the floppy drive at 4.77 MHz. If your
    system is faster than 4.77 MHz, you may experience problems accessing
    the floppy drive when the system is in "turbo" mode. Usually this only
    happens with very old controllers.

TEST_CPU:
    Define to test flags/registers of CPU at power on. If an error is found
    the system will be halted with no error codes or beeps. TEST_CPU must
    be disabled if using ENHANCED_KEYB due to limited ROM space.

TEST_VIDEO:
    Define to test video memory at power on. Note that this applies only to
    monochrome, Hercules, or CGA graphics cards. The system BIOS never tests
    Video memory on EGA/VGA cards. (Video memory on EGA/VGA cards may be
    tested by their own BIOS.)

MAX_MEMORY:
    Set the maximum memory allowed in KB. If not defined, 640 is used.
    Setting a value larger than your system has will not "give" it more
    memory; it only changes the amount that may be detected.

FAST_MEM_CHECK:
    Uses only a single pattern (+clear) to test memory, rather than the
    original three pattern (+clear) check.

NO_MEM_CHECK:
    Does not test any patterns; clears memory only.

CLEAR_UMA:
    Define to clear a specified region in the upper memory area. This is
    useful for systems that can provide non-EMS UMBs. If the UMBs are not
    cleared before use, memory parity errors can occur.

    UMA_START:
        Starting segment. Must be 8K aligned. Uses A000h if not defined.

    UMA_END:
        Ending segment. Must be 8K aligned. Uses F000h if not defined.

ENHANCED_KEYB:
    Define to enable Int 9h enhanced (101-key) keyboard support. This adds
    support for F11/F12 as well as SHIFT + gray cursor and nav keys (Insert,
    Home, etc). Standard 83/84-key XT keyboards can still be used if this
    option is enabled.

BOOT_DELAY:
    The number of seconds to wait after the memory test, before booting
    or starting BASIC. Pressing any key will bypass the delay.

WARM_BOOT_BASIC:
    If defined, will display the ROM BASIC boot prompt during a warm boot
    (i.e. when CTRL+ALT+DEL is pressed). Normally this prompt is only shown
    during a cold boot.

Building
--------
To automate building the BIOS, a batch file named MAKE.BAT is included. This
batch file will assembly and link the BIOS for you. The BIOS image with
correct checksum is output to PCXTBIOS.BIN.

If you'd like to test out the BIOS, you can run PICOXT.EXE in the TEST
directory. (PicoXT is an IBM XT emulator for Windows by Picofactory.)
No configuration is necessary; the new BIOS image is injected into
PICOXT.EXE when it is built by the batch file.

The batch file will also generate (E)EPROM images of the PC/XT BIOS, ROM
BASIC, and optionally external floppy, SCSI, and IDE BIOSes. You can use
these files with an EPROM programmer to "burn" your own BIOS chips for
use with your motherboard. The EPROM images are output to the "EPROMS"
directory, which has the following structure:

\2764
  FDC344.ROM   (8K, optional)
  TMC850M.ROM  (8K, optional)
  WDBIOS.ROM   (8K, optional)
  BASICF6.ROM  (8K)
  BASICF8.ROM  (8K)
  BASICFA.ROM  (8K)
  BASICFC.ROM  (8K)
  PCXTBIOS.ROM (8K)
\27128
  FDC344.ROM   (16K, optional)
  TMC850M.ROM  (16K, optional)
  WDBIOS.ROM   (16K, optional)
  BASICF6.ROM  (16K)
  BASICF8.ROM  (16K)
  BASICFA.ROM  (16K)
  BASICFC.ROM  (16K)
  PCXTBIOS.ROM (16K)
\27256
  FDC344.ROM   (32K, optional)
  TMC850M.ROM  (32K, optional)
  WDBIOS.ROM   (32K, optional)
  BASICF6.ROM  (32K)
  BASICF8.ROM  (32K)
  BASICFA.ROM  (32K)
  BASICFC.ROM  (32K)
  PCXTBIOS.ROM (32K)
\27512
  FDC344.ROM   (64K, optional)
  TMC850M.ROM  (64K, optional)
  WDBIOS.ROM   (64K, optional)
  BASICF6.ROM  (64K)
  BASICF8.ROM  (64K)
  BASICFA.ROM  (64K)
  BASICFC.ROM  (64K)
  PCXTBIOS.ROM (64K)
\IBMXT
  U18.ROM (32K)
  U19.ROM (32K)

XT clones with 8K ROMs
----------------------
The files in 2764, 27128, 27256, and 27512 are for use with motherboards
that have 8K BIOS ROM sockets. (Usually these boards have 5-8 sockets.)
The files are all 8K images, but those in 27128, 27256, and 27512 use
repeating 8K images to fill 16K/32K/64K EPROM chips, which allows them
to work in 8K sockets. So you can, for example, use 27512 EPROMs when
the motherboard expects 2764 EPROMs with no problems.

The PCXTBIOS.ROM is the only required ROM, although it is nice to have
BASIC if your board has the sockets. The BIOS typically goes in the
last socket, which corresponds to segment FE00. The BASIC ROMs go in
the next 4 sockets (FC00, FA00, F800, and F600). If you are using the
external floppy/SCSI/IDE BIOSes, they can go in any of the remaining
sockets.

IBM PC ROM Note
---------------
Unlike virtually all XT clones, the IBM PC (5150) uses 2364 EPROMs instead
of 2764 EPROMs. These are still 8K ROMs but the pinout is slightly different.
If you are having a hard time finding or burning 2364 EPROMs, you can make or
buy an adapter that will let you use 27xxx EPROMs in your IBM PC:

http://www.minuszerodegrees.net/5150_u33.htm
http://store.go4retro.com/2364-adapter/

IBM XT and clones with 32K ROMs
-------------------------------
If you have an IBM XT (5155 or 5160) or clone with two 32K ROM sockets, you
should use the ROM images in the IBMXT folder. U19 is mapped to F000 and U18
is mapped to F800. The first 8K of BASIC is in U19 and the last 24K of BASIC
is in U18 along with the 8K BIOS.

External BIOS Note
------------------
Three external BIOSes are included:

  FDC344  - Ably-Tech Unique 4-Drive Floppy Controller v4.2
  TMC850M - Future Domain TMC-850M SCSI Controller v8.5 (yes, it boots!)
  WDBIOS  - Western Digital IDE SuperBIOS v2.0

To generate corresponding ROM images of these BIOSes, change the defines
at the top of MAKE.BAT to equal 1 instead of 0. For example,

set floppy=1
set scsi=1
set ide=1

Please note that using these external BIOSes without the corresponding
hardware may give unpredictable results. These BIOSes are included for
educational/personal use only and may not be used commercially.


BIOS History from Ya`akov Miles
===============================
From: Ya`akov Miles <multi%dac.triumf.cdn%ubc.csnet at RELAY.CS.NET>
To:   Info-IBMPC at MIT-MC
Re:   BIOS Musings

You may be interested in a history of where this BIOS came from, and how it
arrived in its present form. A heavily patched, partially-functionally BIOS
(with no copyright statement, or other visible indication of origin) was
supplied with my IBM-PC/XT compatible 10 MHz motherboard.

In order to get my motherboard to function correctly, in other words, to
work with the parity interrupt enabled and to operate with the NEC "V20",
it was necessary to disassemble and thoroughly go through this "anonymous"
BIOS, which was hinted as supplied by Taiwan, while limping along on a name
brand BIOS, as supplied on my previous motherboard by a different vendor.

In the course of this disassembly, aided by comparison with the published
IBM-PC/XT listings, it became apparent that the synchronization on
horizontal retrace in the video INT 10h service was the root cause of the
failure to operate with the NEC "V20", and that correcting it to correspond
with logic (as in IBM's BIOS) caused the glitch to disappear. I am unable to
account as to why several name brand BIOS brands (excluding IBM's) had
similar glitches--maybe they they were produced from similar source code,
although this seems unlikely.

In any case, as evidenced by DEBUG, some of these name-brand BIOS were full
of machine-level patches--did the vendor ever bother to reassemble and
optimize the source code? The code that I examined was full of recursive
INT(errupt) instructions, which did not to contribute to screaming fast
BIOS. Therefore, the assembly code was rearranged so as to eliminate some of
the unnecessary CALL, JMP, and especially INT instructions, as the
optimization proceeded with the later releases.

The BIOS is copyright (c) Anonymous, because there was no indication of the
original authors...

ps: While playing around with my 10 MHz motherboard, I encountered an
    unusual program called HELPME.COM, which ran at a higher pitch than
    normal. Since this program behaved normally on other (8 MHz) turbo
    motherboards, my curiosity was aroused. This eventually led me to
    discover that the 10 MHz motherboard was refreshed in hardware by
    channel 1 of the 8253 timer IC, and that this channel appeared to be
    counting down from an unusually fast oscillator. Maybe this could cause
    problems with other programs...
