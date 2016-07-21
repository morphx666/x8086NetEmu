============================================================================
           Turbo XT BIOS for Intel 8088 or NEC "V20" Motherboards
     Additions by Ya`akov Miles (1987) and Jon Petrosky <Plasma> (2008)
                          http://www.phatcode.net/
----------------------------------------------------------------------------
                         Version 2.0 - Apr 23 2008
============================================================================


About
=====
This is a modification of the widely-distributed "(c) Anonymous Generic
Turbo XT Anonymous" BIOS, which is actually a Taiwanese BIOS that was
reverse-engineered by Ya`akov Miles in 1987.

I recently put together an XT system and wanted a BIOS that supported
booting from a hard drive. The Generic XT BIOS did not support this, but
since source code was provided it was easy to add this feature. While I was
at it, I fixed some bugs I found, added more features, and cleaned up the
code. Initially I only modified this BIOS for my computer, but I decided I
might as well release my new version in case it proves useful for someone
else.

This BIOS will work on IBM-PC/XT and many other compatibles that share a
similar design concept. You do not need to have a turbo motherboard to use
this BIOS, but if you do, then you can use the "CTRL ALT -" key combination
to toggle the computer speed between fast and slow. When the speed is
toggled, the PC speaker will sound a high/low pitched blip to indicate the
system speed selected.


Changes
=======
I have made the following changes to Miles' original source code:

Code:
  * Changed from MASM 4.x to TASM IDEAL mode
  * Cleaned up source code: added procs, more descriptive labels
    (...still some spaghetti due to necessary hard-coded entry points)

Bug Fixes:
  * Warm boot flag restored after external ROM initialization code
      (fixes problem where CTRL+ALT+DEL does cold boot instead of warm boot)
  * Equipment Flag in BIOS Data Area now set correctly
  * Fixed cursor shape when using Hercules card
      (was in the middle of the character before)

Added:
  * Optional define to always boot up in turbo mode (TURBO_BOOT)
  * Optional define for fast memory check (FAST_MEM_CHECK); uses one test
      pattern only
  * Optional define for 101-key keyboard shift+nav keys work-around
      (KEYB_SHIFT_FIX)
  * BIOS is now EGA/VGA aware; will only test video memory on
      Mono/Herc/CGA cards
  * Nicer boot screen with color support for CGA/EGA/VGA
  * Processor and Math Coprocessor detection
  * Memory test displays count every 8K; speeds up memory check on fast
      systems with slow video cards
  * User has option to boot ROM BASIC even if bootable disk present
  * Supports booting from hard drive (if external controller ROM present)
  * Toggling turbo on/off sounds high/low-pitched beep rather than
      changing cursor shape


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
The BIOS has built-in support for low-density 5.25" and 3.5" drives only. I
didn't add support for high-density drives since I have a 4-drive floppy
controller with external BIOS that adds this support. Without an external
floppy BIOS, you can still use high-density drives but are limited to
low-density media.

There is another option for high-density drive support, which is to run
DR-DOS (6.x or 7.x, possibly earlier versions as well). DR-DOS has a unique
feature which actually loads software support for high-density drives at
bootup. As long as you have a high-density disk drive, you can boot DR-DOS
from a high-density disk no matter what the BIOS supports.


Building and Installing the BIOS
================================

Source Code
-----------
The source code for the BIOS is provided in the file XTBIOS.ASM. It has
been formatted for a 100-column page with 8-space tabs. The BIOS will
assemble with any version of TASM. (It will not assemble with LZASM; I'm
not quite sure why...)

Once assembled and linked, the actual BIOS image is the last 8K of the
EXE produced, with the exception of the final checksum byte which needs
to be calculated and added to the end. (The checksum byte makes the sum
of all the bytes in the image = 0 mod 256.)

Options
-------
There are five optional defines in the BIOS source code. To enable/disable
them, simply comment or uncomment the definition in the code and then
rebuild the BIOS.

MAX_MEMORY:
    Set the maximum memory allowed in KB. If not defined, 640 is used.
    Setting a value larger than your system has will not "give" it more
    memory; it only changes the amount that may be detected.

SLOW_FLOPPY:
    If defined, will always run the floppy drive at 4.77 MHz. If your
    system is faster than 4.77 MHz, you may experience problems accessing
    the floppy drive when the system is in "turbo" mode. Usually this only
    happens with very old controllers.

KEYB_SHIFT_FIX:
    When using a 101-key XT keyboard (yes, they exist), holding SHIFT while
    pressing a cursor or navigation key (Insert, Home, etc.) may cancel the
    shift, due to dummy scan codes sent by the keyboard. If defined, a fix
    is used to allow SHIFT with these keys.

FAST_MEM_CHECK:
    Uses only a single pattern to test memory, rather than the traditional
    four-pattern check.

TURBO_BOOT:
    If defined, system speed is switched to "turbo" at bootup. This is done
    after hardware detection, but before external ROM initialization and
    memory check. Has no effect on non-turbo systems.

Building
--------
To automate building the BIOS, a batch file named MAKE.BAT is included. This
batch file will assembly and link the BIOS for you. The BIOS image with
correct checksum is output to XTBIOS.BIN.

If you'd like to test out the BIOS, you can run PICOXT.EXE in the TEST
directory. (PicoXT is an IBM XT emulator for Windows by Picofactory.)
No configuration is necessary; the new BIOS image is injected into
PICOXT.EXE when it is built by the batch file.

The batch file will also generate (E)EPROM images of the XT BIOS, ROM
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
  XTBIOS.ROM   (8K)
\27128
  FDC344.ROM   (16K, optional)
  TMC850M.ROM  (16K, optional)
  WDBIOS.ROM   (16K, optional)
  BASICF6.ROM  (16K)
  BASICF8.ROM  (16K)
  BASICFA.ROM  (16K)
  BASICFC.ROM  (16K)
  XTBIOS.ROM   (16K)
\27256
  FDC344.ROM   (32K, optional)
  TMC850M.ROM  (32K, optional)
  WDBIOS.ROM   (32K, optional)
  BASICF6.ROM  (32K)
  BASICF8.ROM  (32K)
  BASICFA.ROM  (32K)
  BASICFC.ROM  (32K)
  XTBIOS.ROM   (32K)
\27512
  FDC344.ROM   (64K, optional)
  TMC850M.ROM  (64K, optional)
  WDBIOS.ROM   (64K, optional)
  BASICF6.ROM  (64K)
  BASICF8.ROM  (64K)
  BASICFA.ROM  (64K)
  BASICFC.ROM  (64K)
  XTBIOS.ROM   (64K)
\SPLIT
  BIOS.0  (32K even split)
  BIOS.1  (32K odd split)

The files in 2764, 27128, 27256, and 27512 are for use with motherboards
that have 8K BIOS ROM sockets. (Usually these boards have 5-8 sockets.)
The files are all 8K images, but those in 27128, 27256, and 27512 use
repeating 8K images to fill 16K/32K/64K EPROM chips, which allows them
to work in 8K sockets. So you can, for example, use 27512 EPROMs when
the motherboard expects 2764 EPROMs with no problems.

The XTBIOS.ROM is the only required ROM, although it is nice to have
BASIC if your board has the sockets. The XT BIOS typically goes in the
last socket, which corresponds to segment FE00. The BASIC ROMs go in
the next 4 sockets (FC00, FA00, F800, and F600). If you are using the
external floppy/SCSI/IDE BIOSes, they can go in any of the remaining
sockets.

Some motherboards, such as the original IBM XT, have two 32K ROM sockets
designed to hold a 64K BIOS image split between two even/odd chips. If
this is the case with your motherboard, you should use the files in the
SPLIT directory. These files include the XT BIOS, ROM BASIC, and optionally
the external floppy/SCSI/IDE BIOSes. Also note that you can use the
BIOS.0 and BIOS.1 files as substitutes for XT050986.0 and XT050986.1, which
some XT emulators require.

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

This will also include these ROMs in the even/odd file in the SPLIT
directory.

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
