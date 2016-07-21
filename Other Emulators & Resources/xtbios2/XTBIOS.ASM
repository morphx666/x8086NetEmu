;===================================================================================================
;                      Turbo XT BIOS for Intel 8088 or NEC "V20" Motherboards
;                Additions by Ya`akov Miles (1987) and Jon Petrosky <Plasma> (2008)
;                                     http://www.phatcode.net/
;---------------------------------------------------------------------------------------------------
; This is a modification of the widely-distributed "(c) Anonymous Generic Turbo XT Anonymous" BIOS,
; which is actually a Taiwanese BIOS that was reverse-engineered by Ya`akov Miles in 1987.
;
; I recently put together an XT system and wanted a BIOS that supported booting from a hard drive.
; The Generic XT BIOS did not support this, but since source code was provided it was easy to add
; this feature. While I was at it, I fixed some bugs I found, added more features, and cleaned up
; the code. Initially I only modified this BIOS for my computer, but I decided I might as well
; release my new version in case it proves useful for someone else.
;
; This BIOS will work on IBM-PC/XT and many other compatibles that share a similar design concept.
; You do not need to have a turbo motherboard to use this BIOS, but if you do, then you can use the
; "CTRL ALT -" key combination to toggle the computer speed between fast and slow. When the speed is
; toggled, the PC speaker will sound a high/low pitched blip to indicate the system speed selected.
;---------------------------------------------------------------------------------------------------
;    File: XTBIOS.ASM
; Updated: 04-23-2008
;===================================================================================================


;---------------------------------------------------------------------------------------------------
; Changes by Plasma
;---------------------------------------------------------------------------------------------------
; Code:
;   * Changed from MASM 4.x to TASM IDEAL mode
;   * Cleaned up source code: added procs, more descriptive labels
;     (...still some spaghetti due to necessary hard-coded entry points)
;
; Bug Fixes:
;   * Warm boot flag restored after external ROM initialization code (fixes CTRL+ALT+DEL warm boot)
;   * Equipment Flag in BIOS Data Area now set correctly
;   * Fixed cursor shape when using Hercules card (was in the middle of the character)
;
; Added:
;   * Optional define to always boot up in turbo mode (TURBO_BOOT)
;   * Optional define for fast memory check (FAST_MEM_CHECK); uses one test pattern only
;   * Optional define for 101-key keyboard shift+nav keys work-around (KEYB_SHIFT_FIX)
;   * BIOS is now EGA/VGA aware; will only test video memory on Mono/Herc/CGA cards
;   * Nicer boot screen with color support for CGA/EGA/VGA
;   * Processor and Math Coprocessor detection
;   * Memory test displays count every 8K; speeds up check on fast systems with slow video cards
;   * User has option to boot ROM BASIC even if bootable disk present
;   * Supports booting from hard drive (if external controller ROM present)
;   * Toggling turbo on/off sounds high/low-pitched beep rather than changing cursor shape
;
; Notes:
;   * High-density floppy disks (3.5" 1.44MB and 5.25" 1.2MB) are not still supported. You will
;     need to use an external floppy controller ROM for this. Another option is to run DR-DOS,
;     which loads software support for high-density drives.
;---------------------------------------------------------------------------------------------------


;---------------------------------------------------------------------------------------------------
; BIOS History from Ya`akov Miles
;---------------------------------------------------------------------------------------------------
; From: Ya`akov Miles <multi%dac.triumf.cdn%ubc.csnet at RELAY.CS.NET>
; To:   Info-IBMPC at MIT-MC
; Re:   BIOS Musings
;
; You may be interested in a history of where     similar source code, although this seems
; this BIOS came from, and how it arrived in      unlikely.
; its present form. A heavily patched,
; partially-functionally BIOS (with no            In any case, as evidenced by DEBUG, some of
; copyright statement, or other visible           these name-brand BIOS were full of machine-
; indication of origin) was supplied with my      level patches--did the vendor ever bother to
; IBM-PC/XT compatible 10 MHz motherboard.        reassemble and optimize the source code? The
;                                                 code that I examined was full of recursive
; In order to get my motherboard to function      INT(errupt) instructions, which did not to
; correctly, in other words, to work with the     contribute to screaming fast BIOS. Therefore,
; parity interrupt enabled and to operate         the assembly code was rearranged so as to
; with the NEC "V20", it was necessary to         eliminate some of the unnecessary CALL, JMP,
; disassemble and thoroughly go through this      and especially INT instructions, as the
; "anonymous" BIOS, which was hinted as           optimization proceeded with the later releases.
; supplied by Taiwan, while limping along on
; a name brand BIOS, as supplied on my            The BIOS is copyright (c) Anonymous, because there
; previous motherboard by a different vendor.     was no indication of the original authors...
;
; In the course of this disassembly, aided by     ps: While playing around with my 10 MHz
; comparison with the published IBM-PC/XT             motherboard, I encountered an unusual program
; listings, it became apparent that the               called HELPME.COM, which ran at a higher
; synchronization on horizontal retrace in            pitch than normal. Since this program behaved
; the video INT 10h service was the root              normally on other (8 MHz) turbo motherboards,
; cause of the failure to operate with the            my curiosity was aroused. This eventually
; NEC "V20", and that correcting it to                led me to discover that the 10 MHz
; correspond with logic (as in IBM's BIOS)            motherboard was refreshed in hardware by
; caused the glitch to disappear. I am unable         channel 1 of the 8253 timer IC, and that this
; to account as to why several name brand             channel appeared to be counting down from an
; BIOS brands (excluding IBM's) had similar           unusually fast oscillator. Maybe this could
; glitches--maybe they were produced from             cause problems with other programs...
;---------------------------------------------------------------------------------------------------


;---------------------------------------------------------------------------------------------------
; Email Header from Original BIOS
;---------------------------------------------------------------------------------------------------
; Date:     Wed, 13 Jan 88 04:26 PST
; From:     <MULTI%TRIUMFCL.BITNET@CUNYVM.CUNY.EDU>
; Subject:  BIOS.ASM update INFO-IBMPC librarys
; To:       hicks@walker-emh.arpa
;---------------------------------------------------------------------------------------------------


ideal
p8086
model tiny
warn
multerrs


;---------------------------------------------------------------------------------------------------
; Error Codes (produced at IPL time)
;---------------------------------------------------------------------------------------------------
error_bios equ 01h  ; Bad ROM BIOS checksum, patch last byte
error_ram equ 02h  ; Bad RAM in main memory, replace
error_video equ 04h  ; Bad RAM in video card, replace
error_mem equ 10h  ; Bad RAM in vector area, replace
error_rom equ 20h  ; Bad ROM in expansion area, bad checksum


;---------------------------------------------------------------------------------------------------
; Miscellaneous Definitions
;---------------------------------------------------------------------------------------------------
MAX_MEMORY = 704  ; Maximum memory allowed in KB
;SLOW_FLOPPY = 1  ; Define to always run floppy drive at 4.77 MHz
KEYB_SHIFT_FIX = 1  ; Define to enable SHIFT+CURSOR/NAV keys fix (for 101-key keyboards)
;FAST_MEM_CHECK = 1  ; Define to use faster but less thorough memory check
TURBO_BOOT = 1  ; Define to boot up in "turbo" mode (full speed)


;---------------------------------------------------------------------------------------------------
; Macros
;---------------------------------------------------------------------------------------------------
; Pad code to create entry point at specified address
macro entry addr
 pad = str_banner - $ + addr - 0E000h
 if pad lt 0
  err 'No room for ENTRY point'
 endif
 if pad gt 0
  db pad dup(0FFh)
 endif
endm

; Force far jump
macro jmpf segm, offs
 db 0EAh;
 dw offs, segm
endm

; Line feed and carriage return
LF equ 0Ah
CR equ 0Dh


;---------------------------------------------------------------------------------------------------
; BIOS Data Area (BDA)
;---------------------------------------------------------------------------------------------------
assume ds:code, ss:code, cs:code, es:code
segment data at 40h    ; IBM compatible data structure

 dw 4 dup(?) ; 40:00  ; RS232 serial COM ports - up to four
 dw 4 dup(?) ; 40:08  ; Parallel LPT ports - up to four
 dw ?  ; 40:10  ; Equipment present word
      ;   Bits 15-14: Number of parallel ports
      ;     00b = 1 parallel port
      ;     01b = 2 parallel ports
      ;     10b = 3 parallel ports
      ;   Bit 13: Reserved
      ;   Bit 12: Game port adapter
      ;     0b = not installed
      ;     1b = installed
      ;   Bits 11-9: Number of serial ports
      ;     000b = none
      ;     001b = 1 serial port
      ;     010b = 2 serial ports
      ;     011b = 3 serial ports
      ;     100b = 4 serial ports
      ;   Bit 8: Reserved
      ;   Bits 7-6: Number of floppy drives
      ;     00b = 1 floppy drive
      ;     01b = 2 floppy drives
      ;     10b = 3 floppy drives
      ;     11b = 4 floppy drives
      ;   Bits 5-4: Initial video mode
      ;     00b = EGA/VGA
      ;     01b = color 40x25
      ;     10b = color 80x25
      ;     11b = monochrome 80x25
      ;   Bit 3-2: System board RAM
      ;     00b = 16K
      ;     01b = 32K
      ;     10b = 48K
      ;     11b = 64K (default)
      ;   Bit 1: Math coprocessor
      ;     0b = not installed
      ;     1b = installed
      ;   Bit 0: Boot floppy
      ;     0b = not installed
      ;     1b = installed
 db ?  ; 40:12  ; MFG test flags, unused by us
 dw ?  ; 40:13  ; Memory size, kilobytes
 db ?  ; 40:15  ; IPL errors<-table/scratchpad
 db ?    ;  ...unused

;------------------------------------------------ Keyboard Data Area

 db ?, ?  ; 40:17  ; Shift/Alt/etc. keyboard flags
 db ?  ; 40:19  ; Alt-KEYPAD char. goes here
 dw ?  ; 40:1A  ;  --> keyboard buffer head
 dw ?  ; 40:1C  ;  --> keyboard buffer tail
 dw 16 dup(?) ; 40:1E  ; Keyboard Buffer (Scan,Value)

;------------------------------------------------ Diskette Data Area

 db ?  ; 40:3E  ; Drive Calibration bits 0 - 3
 db ?  ; 40:3F  ; Drive Motor(s) on 0-3,7=write
 db ?  ; 40:40  ; Ticks (18/sec) til motor off
 db ?  ; 40:41  ; Floppy return code stat byte
      ;  1 = bad IC 765 command request
      ;  2 = address mark not found
      ;  3 = write to protected disk
      ;  4 = sector not found
      ;  8 = data late (DMA overrun)
      ;  9 = DMA failed 64K page end
      ; 16 = bad CRC on floppy read
      ; 32 = bad NEC 765 controller
      ; 64 = seek operation failed
      ;128 = disk drive timed out
 db 7 dup(?) ; 40:42  ; Status bytes from NEC 765

;------------------------------------------------ Video Display Area

 db ?  ; 40:49  ; Current CRT mode  (software)
      ;  0 = 40 x 25 text (no color)
      ;  1 = 40 x 25 text (16 color)
      ;  2 = 80 x 25 text (no color)
      ;  3 = 80 x 25 text (16 color)
      ;  4 = 320 x 200 graphics 4 color
      ;  5 = 320 x 200 graphics 0 color
      ;  6 = 640 x 200 graphics 0 color
      ;  7 = 80 x 25 text (mono card)
 dw ?  ; 40:4A  ; Columns on CRT screen
 dw ?  ; 40:4C  ; Bytes in the regen region
 dw ?  ; 40:4E  ; Byte offset in regen region
 dw 8 dup(?) ; 40:50  ; Cursor pos for up to 8 pages
 dw ?  ; 40:60  ; Current cursor mode setting
 db ?  ; 40:62  ; Current page on display
 dw ?  ; 40:63  ; Base addres (B000h or B800h)
 db ?  ; 40:65  ; IC 6845 mode register (hardware)
 db ?  ; 40:66  ; Current CGA palette

;------------------------------------------------ Used to setup ROM

 dw ?, ?  ; 40:67  ; Eprom base Offset,Segment
 db ?  ; 40:6B  ; Last spurious interrupt IRQ

;------------------------------------------------ Timer Data Area

 dw ?  ; 40:6C  ; Ticks since midnite (lo)
 dw ?  ; 40:6E  ; Ticks since midnite (hi)
 db ?  ; 40:70  ; Non-zero if new day

;------------------------------------------------ System Data Area

 db ?  ; 40:71  ; Sign bit set iff break
 dw ?  ; 40:72  ; Warm boot iff 1234h value

;------------------------------------------------ Hard Disk Scratchpad
 dw ?, ?  ; 40:74

;------------------------------------------------ Timout Areas/PRT/LPT

 db 4 dup(?) ; 40:78  ; Ticks for LPT 1-4 timeouts
 db 4 dup(?) ; 40:7C  ; Ticks for COM 1-4 timeouts

;------------------------------------------------ Keyboard Buffer Start/End

 dw ?  ; 40:80  ; Contains 1Eh, buffer start
 dw ?  ; 40:82  ; Contains 3Eh, buffer end

ends data


;---------------------------------------------------------------------------------------------------
; Segment Definitions
;---------------------------------------------------------------------------------------------------
segment dos_dir at 50h    ; Boot disk directory from IPL

label copy byte    ;   0 if Print Screen idle
      ;   1 if PrtScr copying screen
      ; 255 if PrtScr error in copy
      ;   (non-graphics PrtScr in BIOS)

 db 200h dup(?)   ; PC-DOS bootstrap procedure
      ;   IBMBIO.COM buffers the directory of the boot
      ;   device here at IPL time when locating the guts
      ;   of the operating system (IBMDOS.COM/MSDOS.SYS)
ends dos_dir


segment dos_seg at 70h    ; "Kernel" of DOS

 ; IBMBIO.COM (or IO.SYS) file loaded by boot block. Device Drivers/Bootstrap.
 ; IBMDOS.COM (or MSDOS.SYS) operating system kernel immediately follows IBMBIO.COM and
 ; doesn't have to be contiguous. The DOS kernel binary image is loaded by transient code
 ; in the IBMBIO binary image.

ends dos_seg


segment ipl_seg at 0h    ; Segment for boot block

 ; The following boot block is loaded with 512 bytes of the first sector of the bootable
 ; device by code resident in the ROM BIOS. Control is then transferred to the first word
 ; 0000:7C00 of the disk-resident bootstrap.

 org 07C00h    ;  Offset for boot block
boot db 200h dup(?)   ;  Start disk resident boot

ends ipl_seg


;---------------------------------------------------------------------------------------------------
; ROM BIOS
;---------------------------------------------------------------------------------------------------
segment code

 org 0E000h    ; 8K ROM BIOS starts at F000:E0000


str_banner db 'Turbo XT BIOS for 8088/V20', 0
str_banner_2 db CR, LF, 'Additions by Ya`akov Miles (1987) and Jon Petrosky (2008)', CR, LF, 0


;---------------------------------------------------------------------------------------------------
; BIOS Power-On Self Test (POST)
;---------------------------------------------------------------------------------------------------
 entry 0E05Bh    ; IBM restart entry point
proc post near

cold_boot:
 mov ax, 40h    ; Entered by POWER_ON/RESET
 mov ds, ax
 mov [word ds:72h], 0  ; Show data areas not init

warm_boot:
 cli     ; Begin FLAG test of CPU
 xor ax, ax
 jb @@halt
 jo @@halt
 js @@halt
 jnz @@halt
 jpo @@halt
 add ax, 1
 jz @@halt
 jpe @@halt
 sub ax, 8002h
 js @@halt
 inc ax
 jno @@halt
 shl ax, 1
 jnb @@halt
 jnz @@halt
 shl ax, 1
 jb @@halt

 mov bx, 0101010101010101b  ; Begin REGISTER test of CPU
@@cpu_test:
 mov bp, bx
 mov cx, bp
 mov sp, cx
 mov dx, sp
 mov ss, dx
 mov si, ss
 mov es, si
 mov di, es
 mov ds, di
 mov ax, ds
 cmp ax, 0101010101010101b
 jnz @@cpu_1
 not ax
 mov bx, ax
 jmp @@cpu_test

@@cpu_1:
 xor ax, 1010101010101010b
 jz @@cpu_ok

@@halt: hlt

@@cpu_ok:
 cld
 mov al, 0    ; Prepare to initialize
 out 0A0h, al   ;   no NMI interrupts
 mov dx, 3D8h   ; Load Color Graphic port
 out dx, al    ;   no video display
 mov dx, 3B8h   ; Load Monochrome port
 inc al    ;   no video display
 out dx, al    ;   write it out
 mov al, 10011001b   ; Program 8255 PIA chip
 out 63h, al    ;   Ports A & C, inputs
 mov al, 10100101b   ; Set (non)turbo mode
 out 61h, al    ;   on main board
 mov al, 01010100b   ; IC 8253 inits memory refresh
 out 43h, al    ;   chan 1 pulses IC 8237 to
 mov al, 12h    ;   DMA every 12h clock ticks
 out 41h, al    ;   64K done in 1 millisecond
 mov al, 01000000b   ; Latch value 12h in 8253 clock
 out 43h, al    ;   chip channel 1 counter

@@init_dma:
 mov al, 0    ; Do some initialization
 out 81h, al    ;   DMA page reg, chan 2
 out 82h, al    ;   DMA page reg, chan 3
 out 83h, al    ;   DMA page reg, chan 0,1
 out 0Dh, al    ; Stop DMA on 8237 chip
 mov al, 01011000b   ; Refresh auto-init dummy read
 out 0Bh, al    ;   on channel 0 of DMA chip
 mov al, 01000001b   ; Block verify
 out 0Bh, al    ;   on channel 1 of DMA chip
 mov al, 01000010b   ; Block verify
 out 0Bh, al    ;   on channel 2 of DMA chip
 mov al, 01000011b   ; Block verify
 out 0Bh, al    ;   on channel 3 of DMA chip
 mov al, 0FFh   ; Refresh byte count
 out 1, al    ;   send lo order
 out 1, al    ;   send hi order
 mov al, 0    ; Initialize 8237 command reg
 out 8, al    ;   with zero
 out 0Ah, al    ; Enable DMA on all channels
 mov al, 00110110b   ; Set up 8253 timer chip
 out 43h, al    ;   chan 0 is time of day
 mov al, 0    ; Request a divide by
 out 40h, al    ;   65536 decimal
 out 40h, al    ;   0000h or 18.2 tick/sec
 mov dx, 213h   ; Expansion unit port
 mov al, 1    ;   enable it
 out dx, al    ;   do the enable
 mov ax, 40h    ; Get BIOS impure segment
 mov ds, ax    ;   into ds register
 mov si, [ds:72h]   ; Save reset flag in si reg
 xor ax, ax    ;   cause memory check
 mov bp, ax    ;   will clobber the flag
 mov bx, ax    ; Start at segment 0000h
 mov dx, 55AAh   ;   get pattern
 cld     ; Strings auto-increment

@@find_mem_size:
 xor di, di    ; Location XXXX:0
 mov es, bx    ;   load segment
 mov [es:di], dx   ;   write pattern
 cmp dx, [es:di]   ;   compare
 jnz @@done_mem_size   ;   failed, memory end
 mov cx, 2000h   ; Else zero 16 kilobytes
 repz stosw    ;   with instruction
 add bh, 4    ;   get next 16K bytes
ifdef MAX_MEMORY
 cmp bh, MAX_MEMORY shr 2  ; Found max legal user ram?
else
 cmp bh, 0A0h   ; Found max legal IBM ram?
endif
 jnz @@find_mem_size   ;   no, then check more

@@done_mem_size:
 xor ax, ax
 mov es, ax    ; es = vector segment
 mov ax, 80h
 mov ss, ax    ; Set up temporary stack at
 mov sp, 100h   ;   0080:0100 for memory check
 push bp
 push bx
 mov bp, 2
 call mem_test   ; Memory check es:0 - es:0400
 mov [ds:72h], si   ; Restore reset flag											E14D
 pop ax
 mov cl, 6
 shr ax, cl
 mov [ds:13h], ax
 pop ax
 jnb @@vector_ram_ok
 or al, error_mem   ; Show vector area bad

@@vector_ram_ok:
 mov [ds:15h], al   ; Save IPL error code
 xor ax, ax
 push ax
 push ax
 push ax
 push ax
 push ax
 mov ax, 30h    ; Set up IBM-compatible stack
 mov ss, ax    ;   segment 0030h
 mov sp, 100h   ;   offset  0100h
 push ds															E170
 mov bx, 0E000h   ; Check BIOS eprom
 push cs
 pop ds    ;   at F000:E000														
 mov ah, 1
 call checksum   ;   for valid checksum			E178
 pop ds    ;   restore impure<-ds
 jz @@init_int
 or [byte ds:15h], error_bios ; Checksum error BIOS eprom

@@init_int:
 cli     ; Init interrupt controller
 mov al, 13h
 out 20h, al
 mov al, 8
 out 21h, al
 mov al, 9
 out 21h, al
 mov al, 0FFh
 out 21h, al
 push ds
 xor ax, ax    ; 8 nonsense vectors begin table
 mov es, ax    ;   at segment 0000h
 push cs
 pop ds
 mov cx, 8    ; Vectors 00h - 07h unused
 xor di, di    ;   we start at vec 00h

@@low_vectors:
 mov ax, offset ignore_int  ; Nonsense interrupt from RSX
 stosw
 mov ax, cs    ;   BIOS ROM segment
 stosw
 loop @@low_vectors

 mov si, offset vectors  ; si --> Vector address table
 mov cx, 18h    ;   vectors 08h - 1Fh busy

@@high_vectors:
 movsw     ; Set interrupt vector offset
 mov ax, cs
 stosw     ; Set interrupt vector segment (BIOS segment)
 loop @@high_vectors

 mov ax, 0F600h   ; ax --> ROM BASIC segment							E1B5
 mov ds, ax    ; ds -->  " "     "
 xor bx, bx    ; bx  =  ROM BASIC offset
 mov ah, 4    ; Four BASIC ROMs to check

 mov bp, sp    ; Save the stack pointer
 push cs    ;   push code segment
 mov dx, offset @@skip   ; Save the code offset
 push dx    ;   for RAM_PATCH subroutine
 mov dx, 0EA90h   ; mov dx, 'nop, JMP_FAR'
 push dx    ;   save it on stack
 mov dx, 0178Bh   ; mov dx, 'mov dx, [bx]'
 push dx    ;   save it on stack
 push ss    ; Save stack segment
 mov dx, sp    ;   get the stack offset
 add dx, 02h    ;   calculate xfer address
 push dx    ;   save it on the stack

 retf     ; Test for BASIC rom											E1D4
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; mov dx, [bx]    ; Executes off the stack ;
; jmpf 0F000h, skip   ;   in RAM space         ;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
@@skip: mov sp, bp    ; Restore the stack pointer				E1D5
 cmp dl, dh    ;   compare 1st and 2nd byte
 je @@kosher   ;   perfection. No piracy

@@basic_rom:
 call checksum   ; Scan for BASIC roms
 jnz @@kosher   ;   bad BASIC rom
 dec ah    ; Continue
 jnz @@basic_rom   ;   yes, more

 pop ds    ; Else valid BASIC
 mov di, 60h    ;   install BASIC

 xor ax,ax    ;   0000h BASIC interrupt offset
 stosw
 mov ax, 0F600h   ;   F600h BASIC interrupt segment
 stosw

 push ds
@@kosher:
 pop ds    ; Setup special low vectors								E1F0
 mov [word es:8], offset int_2 ;   NMI interrupt
 mov [word es:14h], offset int_5 ;   print screen interrupt
 mov [word es:7Ch], 0  ; No special graphics characters
 mov [word es:7Eh], 0  ;   so zero vector 1Fh
 mov dx, 61h
 in al, dx    ; Read machine flags									E210
 or al, 00110000b   ;   clear old parity error
 out dx, al    ; Write them back to reset
 and al, 11001111b   ;   enable parity
 out dx, al    ; Write back, parity enabled
 mov al, 80h    ;   allow NMI interrupts
 out 0A0h, al
 mov ax, 0000000000110000b  ; Assume monochrome video
 mov [ds:10h], ax   ;   card has been installed						E21E
 int 10h    ;   initialize if present 								E221
 mov ax,0000000000100000b  ; Assume color/graphics video
 mov [ds:10h], ax   ;   card has been installed
 int 10h    ;   initialize if present								E229
 in al, 62h    ; Get memory size (64K bytes)
 and al, 00001111b   ;   in bits 2,3 low nibble
 mov ah, al    ; Save memory size nibble
 mov al, 10101101b
 out 61h, al
 in al, 62h    ; Get number of floppies (0-3)
 mov cl, 4    ;   and init video mode
 shl al, cl    ;   shift in hi nibble
 or al, ah
 mov ah, 0
 mov [ds:10h], ax   ; Start building Equipment Flag
 and al, 00110000b   ;   if video card, mode set
 jnz @@video_found   ;   found video interface
 mov ax, offset dummy_int  ; No hardware, dummy_int becomes
 mov [es:40h], ax   ;   int_10 video service
 jmp short @@skip_video

@@video_found:
 call video_init   ; Setup video

@@skip_video:
 mov al, 00001000b   ; Read low switches
 out 61h, al

 mov cx, 2956h
@@keyb_delay:
 loop @@keyb_delay

 mov al, 11001000b   ; Keyboard acknowledge
 out 61h, al    ;   send the request
 xor al, 10000000b   ; Toggle to enable
 out 61h, al    ;   send key enable
 mov ax, 1Eh    ; Offset to buffer start
 mov [ds:1Ah], ax   ; Buffer head pointer
 mov [ds:1Ch], ax   ; Buffer tail pointer
 mov [ds:80h], ax   ; Buffer start
 add ax, 20h    ;   size
 mov [ds:82h], ax   ; Buffer end
 mov ax, 1414h   ; Time-out value seconds
 mov [ds:78h], ax   ;   LPT1
 mov [ds:7Ah], ax   ;   LPT2
 mov ax, 101h   ; Time-out value seconds
 mov [ds:7Ch], ax   ;   COM1
 mov [ds:7Eh], ax   ;   COM2
 mov si, offset lpt_ports  ; si --> LPT port table
 xor di, di    ;   offset into data seg
 mov cx, 3    ;   number of printers

@@next_lpt:
 mov dx, [cs:si]   ; Get LPT port
 mov al, 10101010b   ;   write value
 out dx, al    ;   to the LPT
 mov al, 11111111b   ; Dummy data value
 out 0C0h, al   ;   on the bus
 in al, dx    ; Read code back
 cmp al, 10101010b   ;   check code
 jnz @@no_lpt    ;   no printer found
 mov [di+8], dx   ; Save printer port
 inc di
 inc di

@@no_lpt:
 inc si
 inc si
 loop @@next_lpt

 mov ax, di    ; Number of printers * 2
 mov cl, 3    ;   get shift count
 ror al, cl    ;   divide by eight
 mov [ds:11h], al   ;   save in equipment flag

 xor di, di    ; COM port(s) at 40:00 (hex)

@@com_1:													;	E2B2
 mov dx, 3FBh   ; COM #1 line control reg.
 mov al, 00011010b   ;   7 bits, even parity
 out dx, al    ; Reset COM #1 line cont. reg
 mov al, 11111111b   ;   noise pattern
 out 0C0h, al   ; Write pattern on data buss
 in al, dx    ;   read result from COM #1
 cmp al, 00011010b   ; Check if serial port exists
 jnz @@com_2    ;   skip if no COM #1 port
 mov [word di], 3F8h   ; Else save port # in impure
 inc di    ;   potential COM #2 port
 inc di    ;   is at 40:02 (hex)

@@com_2:
 mov dx, 2FBh   ; COM #2 line control reg
 mov al, 00011010b   ;   7 bits, even parity
 out dx, al    ; Reset COM #2 line cont. reg
 mov al, 11111111b   ;   noise pattern
 out 0C0h, al   ; Write pattern on data bus
 in al, dx    ;   read results from COM #2
 cmp al, 00011010b   ; Check if serial port exists
 jnz @@com_done   ;   skip if no COM #2 port
 mov [word di], 2F8h   ; Else save port # in impure
 inc di    ;   total number of serial
 inc di    ;   interfaces times two

@@com_done:
 mov ax, di    ; Get serial interface count
 or [ds:11h], al   ;   equipment flag

 mov cx, 100    ; Check for game port 100 times				E2E2
 mov dx, 201h
@@check_game:
 in al, dx    ; Anything there?
 cmp al, 0FFh
 jne @@found_game   ; Yep, game port is present
 dec cx    ; Otherwise keep polling
 jcxz @@game_done   ; Countdown is zero; no game port
 jmp @@check_game
@@found_game:
 or [byte ds:11h], 00010000b ; Set flag in equipment word
@@game_done:

 call fpu_check   ; check for FPU

 mov dx, 0C000h   ; ROM segment start

 mov bx, [ds:72h]   ; Save warm boot flag
 push bx    ;   (Some ROM inits may trash it)
 push ds

ifdef TURBO_BOOT    ; if defined enable turbo speed at bootup
 in al, 61h    ; Read equipment flags
 xor al, 00001100b   ;   toggle speed
 out 61h, al    ; Write new flags back
endif

; Break points E354 E34A

@@find_rom:
 mov ds, dx    ; Load ROM segment					E309
 xor bx, bx    ;   ID offset
 mov ax, [bx]   ; Read the ROM ID
 cmp ax, 0AA55h
 jnz @@next_rom   ;   not valid ROM
 mov ax, 40h
 mov es, ax
 mov ah, 0
 mov al, [bx+2]   ; Get ROM size (bytes * 512)
 mov cl, 5
 shl ax, cl    ; Now ROM size in segments
 add dx, ax    ;   add base segment
 mov cl, 4
 shl ax, cl    ; ROM address in bytes
 mov cx, ax    ;   checksum requires cx
 call checksum_entry   ; Find ROM checksum
 jnz @@bad_rom   ;   bad ROM
 push dx
 mov [word es:67h], 3  ; Offset for ROM being setup
 mov [es:69h], ds   ; Segment for ROM being setup
 call [dword es:67h]   ;   call ROM initialization
 pop dx
 jmp short @@continue_rom

@@bad_rom:
 or [byte es:15h], error_rom ; ROM present, bad checksum

@@next_rom:
 add dx, 80h    ; Segment for next ROM

@@continue_rom:
 cmp dx, 0F600h   ; End of ROM space?
 jl @@find_rom   ;  no, continue

 pop ds
 pop bx
 mov [ds:72h], bx   ; Restore warm boot flag

 in al, 21h    ; Read 8259 interrupt mask
 and al, 10111100b   ;   enable IRQ (0, 1, 6) ints
 out 21h, al    ; (tod_clock, key, floppy_disk)

 mov ax, 40h    ; Test for EGA/VGA							E360
 mov es, ax
 mov ah, 12h
 mov bx, 0FF10h
 int 10h    ; Video Get EGA Info							E365
 cmp bh, 0FFh   ; If EGA or later present BH != FFh
 je @@not_ega
 and [byte es:10h], 11001111b ; Set video flag in equipment list to EGA/VGA
@@not_ega:
 push es
 call video_mem_test   ; Do video memory test E378
 pop es

 mov ah, 1
 mov ch, 0F0h
 int 10h    ; Set cursor type
 call clear_screen   ;   clear display
 push cs
 pop ds

 test [byte es:10h], 1  ; Floppy disk present?
 jz @@skip_config   ;   no
 cmp [word es:72h], 1234h  ; Bios setup before?
 jne @@config   ;   no
@@skip_config:
 jmp @@reset    ; Else skip memory check

@@config:
 mov si, offset str_banner
 call color_print
 mov si, offset str_banner_2
 call print

 test [byte es:15h], 11111111b ; Any errors so far?
 jz @@no_errors   ;   no, skip

 mov ax, 0300h
print_error:
 call locate
 mov si, offset str_error
 call print    ; Print string
 mov al, [es:15h]   ;   get error number
 call number    ;   print hex value
 call print    ;   print prompt
 mov bl, 2    ;   long beep
 call beep
 call get_key    ; Wait for keypress
 push ax    ;   save answer
 call out_char   ;   echo answer
 pop ax    ;   get  answer
 cmp al, 'Y'    ; Was it "Y"?
 jz @@no_errors   ;   ok, continue
 cmp al, 'y'    ; Was it "y"?
 jz @@no_errors   ;   ok, continue
 jmpf 0F000h, cold_boot  ; Else cold reset

@@no_errors:
 mov ax, 0300h   ; Where to move cursor							E3DF
 call locate    ; Position cursor
 call display_cpu   ; Display CPU type

 mov si, offset str_mono  ; Assume mono video
 mov ax, 0407h
 call locate
 mov al, [es:49h]   ; Get CRT mode
 cmp al, 7    ; Is it mono?
 jz @@display_video   ; Yes
 mov al, [es:10h]   ; Check equipment word
 and al, 00110000b   ; Is it EGA/VGA?
 jnz @@is_cga   ; No, we have CGA
 mov si, offset str_ega_vga  ; Otherwise we have EGA/VGA
 jmp short @@display_video
@@is_cga:
 mov si, offset str_cga
@@display_video:
 call print    ; Print video adapter present

 mov bx, 0507h
 mov al, [es:11h]   ; Get equipment byte
 push ax
 mov cl, 6
 ror al, cl
 and al, 3    ; Number of LPT ports
 jz @@display_com_ports  ; Skip if none
 mov bp, 8
 mov si, offset str_parallel
 call formatted_ascii_output  ; Formatted ASCII output

@@display_com_ports:
 pop ax
 push ax
 mov si, offset str_serial
 ror al, 1    ; Check for COM ports
 and al, 3
 jz @@display_game_port  ; Skip if none
 xor bp, bp
 call formatted_ascii_output  ; Formatted ASCII output

@@display_game_port:
 pop ax    ; Equipment byte restore
 mov si, offset str_game
 test al, 00010000b   ; Check for game port
 jz @@display_timer   ; Skip if none
 mov ax, bx
 call locate    ; Position cursor
 call print    ;   and print string
 inc bh    ;   scroll line

@@display_timer:
 call timer_check   ; Check for timer device						E447
 jb @@finish_device_list  ; Skip if none
 mov ax, bx
 call locate    ; Position cursor
 inc bh
 mov si, offset str_timer
 call print

@@finish_device_list:
 dec bh
 mov bl, 7
 call fix_last_line_char

 inc bh
 inc bh
 xor bl, bl
 mov ax, bx    ; Where to position cursor					E466
 call locate    ;   position cursor
 mov si, offset str_ram_test  ; Memory size string
 call print    ;   print string

 push es													;E471
 mov bp, [es:13h]   ; Memory size (1 K blocks)
 dec bp
 dec bp
 mov si, 2
 mov dx, si
 mov ax, 80h
 mov es, ax

 add bl, 0Dh
 push bx
@@mem_count:
 pop ax    ; Cursory check of memory
 push ax

 mov cx, es

 cmp bp, 1    ; Always show final memory size
 je @@last_time

 test cx, 0000000111111111b  ; Only print memoy size every 8K
 jz @@show_mem_size

 xor ch, ch    ; If ch is cleared memory size isn't printed
@@show_mem_size:
 dec dx
@@last_time:
 call locate    ; Position cursor
 call print_mem_kb   ; Print size in K
 inc dx

 call mem_test   ; Memory check es:0 - es:0400
 jb @@bad_ram   ;   bad RAM found  (How ???)

 dec bp
 jnz @@mem_count
 pop bx
 pop es

 ; E584
 call boot_basic   ; Boot BASIC if space pressed

@@reset:
 mov bl, 1    ; Do a warm boot
 call beep    ;   short beep
 call clear_screen   ;   clear display
 mov ax, 40h
 mov es, ax
 mov [word es:72h], 1234h  ; Show cold start done

 mov ah, 1
 mov cx, 0B0Ch   ; Set underline cursor mono
 cmp [byte es:49h], 7  ; Get CRT mode
 jz @@is_mono   ;   monochrome
 mov cx, 607h   ; Set underline cursor color
@@is_mono:
 int 10h    ; Set the correct cursor shape

 int 19h    ; Boot the machine

@@bad_ram:
 dec bp
 pop bx
 pop es
 or [byte es:15h], error_ram ; Show "Bad RAM" error
 inc bh
 inc bh
 xor bl, bl
 mov ax, bx
 jmp print_error

endp post


;---------------------------------------------------------------------------------------------------
; Displays string (si) with port number (al) converted to ASCII hex
;---------------------------------------------------------------------------------------------------
proc formatted_ascii_output near

 mov dl, al    ; Formatted ASCII output
@@loop:
 mov ax, bx    ; Get position for
 call locate    ;   cursor routine
 push si    ; Get string address
 call print    ;   print string
 mov ax, [es:bp+0]   ; Get port # to print
 call big_number   ;   four digits
 mov si, offset str_h
 call print
 pop si    ; Restore string address
 inc bp    ;   Address of port
 inc bp    ;   is two bytes long
 inc bh    ;   down one line
 dec dl    ; Decrement device count
 jnz @@loop    ;   back for more
 ret

endp formatted_ascii_output


;---------------------------------------------------------------------------------------------------
; Increase memory size and display size if ch=0. Called during memory test at bootup.
;---------------------------------------------------------------------------------------------------
proc print_mem_kb near

 clc     ; Clear carry flag
 mov al, dl    ;   Size "checked"
 inc al    ;   Show more
 daa
 mov dl, al
 jnb @@skip_carry
 mov al, dh    ;  Do carry
 adc al, 0
 daa
 mov dh, al
@@skip_carry:
 cmp ch, 0
 jz @@done
 mov al, dh
 call digit    ; Print hex digit
 mov al, dl
 mov cl, 4
 ROR al, cl
 call digit    ; Print hex digit
 mov al, dl
 call digit    ; Print hex digit
@@done:
 ret

endp print_mem_kb


;---------------------------------------------------------------------------------------------------
; Check for presence of hardware timer (cf=0 if timer present)
;---------------------------------------------------------------------------------------------------
proc timer_check near

 mov dx, 241h   ; Check for timer #2 port
 cli
 in al, dx    ;   read BCD seconds/100
 sti
 cmp al, 99h    ; Are BCD digits in range?
 jbe @@done    ;   yes, port exists

 mov dx, 341h   ; Check for timer #1 port
 cli
 in al, dx    ;   read BCD seconds/100
 sti
 cmp al, 99h    ; Are BCD digits in range?
 jbe @@done    ;   yes, port exists

 stc     ; No hardware, ports 0FFh
 ret
@@done:
 clc     ; Found timer(s) answering
 ret

endp timer_check


;---------------------------------------------------------------------------------------------------
; Test video memory (Mono/Herc/CGA only, EGA/VGA skips memory test)
;---------------------------------------------------------------------------------------------------
proc video_mem_test near

 mov bp, 4    ; Assume monochrome, 4K memory
 mov bx, 0B000h   ;   segment in bx
 mov al, [ds:49h]   ; Get the video mode
 cmp al, 7    ;   was it mono?
 jz @@do_test   ;   yes, skip

 test [byte es:10h], 00110000b ; Do we have EGA/VGA?
 jz @@done    ; Yes, so skip the video memory check

 mov bp, 10h    ; Else CGA, has 16K memory
 mov bx, 0B800h   ;   segment in bx

@@do_test:
 push bx    ; Load video seg in es
 pop es
 mov al, [ds:65h]   ; Get CRT hardware mode
 and al, 11110111b   ;   disable video
 mov dx, [ds:63h]   ; Get 6845 index port
 add dx, 4    ;   add offset for
 out dx, al    ; 6845 controller port

@@loop: ; E577
 call mem_test   ; Memory check es:0 - es:0400
 dec bp
 jnz @@loop    ; Loop until Video RAM checked
 jnb @@test_ok
 or [byte ds:15h], error_video ; Set Video RAM error in status
@@test_ok:
 call video_init ; E584
@@done:
 ret

endp video_mem_test


;--------------------------------------------------------------------------------------------------
; Display value in ax as 4-character ASCII hex number
;--------------------------------------------------------------------------------------------------
proc double_number

 push ax    ; Unsigned word
 mov al, ah
 call number
 jmp big_number_entry

endp double_number


lpt_ports dw 03BCh, 0378h, 0278h ; Possible line printer ports

str_ram_test db 'Testing RAM:    K OK', 0
str_mono db 195, ' Mono/Hercules Graphics', 0
str_ega_vga db 195, ' EGA/VGA Graphics', 0
str_parallel db 195, ' Parallel Port at ', 0
str_game db 195, ' Game Port at 201h', 0


;---------------------------------------------------------------------------------------------------
; Initial Program Load. Tries to boot from floppy first, then hard drive (external BIOS required
; for hard drive). If both fail ROM BASIC is run if present.
;---------------------------------------------------------------------------------------------------
 entry 0E600h    ; Not necessary to IPL here...
proc ipl near

 sti     ; Called to reboot computer
 xor ax, ax
 mov ds, ax
 mov [word ds:78h], offset int_1E ; Get disk parameter table
 mov [ds:7Ah], cs   ;   save segment
 mov ax, 8    ; Try up to 8 times (4 floppy, 4 hard)

@@retry:
 push ax    ; Save retry count

 xor dx, dx    ; Assume floppy drive
 cmp al, 4    ; Tries above 4?
 ja @@try    ; Yes, use floppy
 or dx, 80h    ; Otherwise set bit to use hard drive
@@try:
 push dx									; E61D

 mov ah, 0
 int 13h    ; Reset drive
 jb @@failed
 mov al, 1    ; One sector
 mov ah, 2    ;   read

 xor dx, dx    ;   from drive 0, head 0
 mov es, dx    ;   segment 0
 pop dx

 mov bx, 7C00h   ;   offset  7C00
 mov cl, 1    ;   sector 1
 mov ch, 0    ;   track  0
 int 13h
 jb @@failed
 jmpf 0000h, 7C00h   ; Call the boot block

@@failed:
 pop ax    ; Get retries
 dec al    ;   one less
 jnz @@retry

@@no_disk:
 or ah, ah    ; Disk present?
 jnz @@disk_error   ;   yes

 push cs
 pop ds
 mov si, offset str_insert_disk ; Load disk message
 call print    ;   and print string
 call get_key    ;   wait for keypress

 mov ax, 0FF08h   ; Reset retry count
 jmp @@retry    ;   and retry

@@disk_error:
 xor ax, ax    ; Error from NEC 765
 mov ds, ax
 les ax, [dword ds:60h]  ; ROM basic vector es:ax
 mov bx, es    ;   get ROM basic segment
 cmp ax, 0
 mov ax, 0
 jnz @@no_disk   ; No ROM basic found
 cmp bx, 0F600h
 jnz @@no_disk   ; Invalid ROM basic segment
 int 18h    ;   else call ROM basic

endp ipl


;---------------------------------------------------------------------------------------------------
; Clear display screen
;---------------------------------------------------------------------------------------------------
proc clear_screen near

 mov dx, 184Fh   ; Lower right corner of scroll
 xor cx, cx    ; Upper left  corner of scroll
 mov ax, 600h   ; Blank entire window
 mov bh, 7    ; Set regular cursor
 int 10h    ; Call video service scroll
 mov ah, 2    ; Set cursor position
 xor dx, dx    ;   upper left corner
 mov bh, 0    ;   page 0
 int 10h    ;   call video service
 mov ax, 0500h   ; Set active display page zero
 int 10h
 ret

endp clear_screen


;---------------------------------------------------------------------------------------------------
; Display CPU present in system (8088 or V20)
;---------------------------------------------------------------------------------------------------
proc display_cpu near

 mov si, offset str_system
 call print
 call cpu_check
 call print

 mov si, offset str_no_fpu
 test [byte es:10h], 00000010b
 jz @@no_fpu
 mov si, offset str_8087
@@no_fpu:
 call print
 ret

endp display_cpu


str_insert_disk  db 'Insert BOOT disk in A:', CR, LF
   db 'Press any key when ready', CR, LF, CR, LF, 0

str_8088 db '8088 CPU (', 0
str_v20  db 'V20 CPU (', 0


;---------------------------------------------------------------------------------------------------
; Interrupt 19h - Warm Boot
;---------------------------------------------------------------------------------------------------
 entry 0E6F2h    ; IBM entry point for int 19h
proc int_19 far

 jmp ipl    ; Warm boot

endp int_19


;---------------------------------------------------------------------------------------------------
; Detect CPU type (8088 or V20)
;---------------------------------------------------------------------------------------------------
proc cpu_check near   ; Test for 8088 or V20 CPU

 xor al, al    ; Clean out al to set ZF
 mov al, 40h    ; mul on V20 does not affect the zero flag
 mul al    ;   but on an 8088 the zero flag is used
 jz @@have_v20   ; Was zero flag set?
 mov si, offset str_8088  ;   No, so we have an 8088 CPU
 ret
@@have_v20:
 mov si, offset str_v20  ;   Otherwise we have a V20 CPU
 ret

endp cpu_check


;---------------------------------------------------------------------------------------------------
; Detect if a FPU/NPU (8087) is present
;---------------------------------------------------------------------------------------------------
proc fpu_check near   ; Test for FPU

 fninit     ; Try to init FPU
 mov si, 0200h
 mov [byte si+1], 0   ; Clear memory byte
 fnstcw [word si]   ; Put control word in memory
 mov ah, [si+1]
 cmp ah, 03h    ; If ah is 03h, FPU is present
 jne @@no_8087
 or [byte ds:10h], 00000010b ; Set FPU in equp list
 ret
@@no_8087:
 and [byte ds:10h], 11111101b ; Set no FPU in equp list
 ret

endp fpu_check


;---------------------------------------------------------------------------------------------------
; Interrupt 14h - Serial RS232 Communications
;---------------------------------------------------------------------------------------------------
 entry 0E729h    ; IBM entry point for int 14h

baud dw 0417h    ;  110 baud clock divisor
 dw 0300h    ;  150 baud clock divisor
 dw 0180h    ;  300 baud clock divisor
 dw 00C0h    ;  600 baud clock divisor
 dw 0060h    ; 1200 baud clock divisor
 dw 0030h    ; 2400 baud clock divisor
 dw 0018h    ; 4800 baud clock divisor
 dw 000Ch    ; 9600 baud clock divisor

proc int_14 far

 sti     ; Serial RS232 COM services
 push ds    ;   through 8250 UART (ugh)
 push dx    ;   dx = COM device (0 - 3)
 push si
 push di
 push cx
 push bx
 mov bx, 40h
 mov ds, bx
 mov di, dx    ;
 mov bx, dx    ; RS232 serial COM index (0-3)
 shl bx, 1    ;   index by bytes
 mov dx, [bx]   ; Convert index to port number
 or dx, dx    ;   by indexing 40:0
 jz @@end    ;   no such COM device, exit
 or ah, ah    ; Init on ah=0
 jz @@init
 dec ah
 jz @@send    ; Send on ah=1
 dec ah
 jz @@receive   ; Receive on ah=2
 dec ah
 jz @@status   ; Status on ah=3

@@end:
 pop bx    ; End of COM service
 pop cx
 pop di
 pop si
 pop dx
 pop ds
 iret

@@init:
 push ax    ; Init COM port, al has data
      ; = (Word Length in Bits - 5)
      ;  +(1 if two stop bits) *  4
      ;  +(1 if parity enable) *  8
      ;  +(1 if parity even  ) * 16
      ;  +(BAUD: select 0-7  ) * 32
 mov bl, al
 add dx, 3    ; Line Control Register (LCR)
 mov al, 80h    ;   index RS232_BASE + 3
 out dx, al    ; Tell LCR to set (latch) baud
 mov cl, 4
 rol bl, cl    ; Baud rate selects by words
 and bx, 00001110b   ;   mask off extraneous
 mov ax, [word cs:bx+baud]  ; Clock divisor in ax
 sub dx, 3    ; Load in lo order baud rate
 out dx, al    ;   index RS232_BASE + 0
 inc dx    ; Load in hi order baud rate
 mov al, ah
 out dx, al    ;   index RS232_BASE + 1
 pop ax
 inc dx    ; Find Line Control Register
 inc dx    ;   index RS232_BASE + 3
 and al, 00011111b   ; Mask out the baud rate
 out dx, al    ;   set (censored) init stat
 mov al, 0
 dec dx    ; Interrupt Enable Reg. (IER)
 dec dx    ;   index RS232_BASE + 1
 out dx, al    ; Interrupt is disabled
 dec dx
 jmp short @@status   ; Return current status

@@send:
 push ax    ; Send al through COM port
 mov al, 3
 mov bh, 00110000b   ; (Data Set Ready, Clear To Send)
 mov bl, 00100000b   ;   (Data Terminal Ready) wait
 call @@wait    ; Wait for transmitter to idle
 jnz @@timeout   ;   time-out error
 sub dx, 5    ;   (xmit) index RS232_BASE
 pop cx    ; Restore char to cl register
 mov al, cl    ;   get copy to load in UART
 out dx, al    ;   transmit char to 8250
 jmp @@end    ;   ah register has status

@@timeout:
 pop cx    ; Transmit error, restore char
 mov al, cl    ;   in al for compatibility
      ;   fall through to generate error
@@timeout_2:
 or ah, 80h    ; Set error (=sign) bit in ah
 jmp @@end    ;   common exit

@@receive:
 mov al, 1    ; Get char. from COM port
 mov bh, 00100000b   ; Wait on DSR (Data Set Ready)
 mov bl, 00000001b   ; Wait on DTR (Data Terminal Ready)
 call @@wait    ;   wait for character
 jnz @@timeout_2   ;   time-out error
 and ah, 00011110b   ; Mask ah for error bits
 sub dx, 5    ;   (receiver) index RS232_BASE
 in al, dx    ; Read the character
 jmp @@end    ;   ah register has status

@@status:
 add dx, 5    ; Calculate line control stat
 in al, dx    ;   index RS232_BASE + 5
 mov ah, al    ;   save high order status
 inc dx    ; Calculate modem stat. reg.
 in al, dx    ;   index RS232_BASE + 6
 jmp @@end    ;   save low  order status
      ; ax = (DEL Clear_To_Send ) *     1
      ;      (DEL Data_Set_ready) *     2
      ;      (Trailing_Ring_Det.) *     4
      ;      (DEL Carrier_Detect) *     8
      ;      (Clear_To_Send     ) *    16
      ;      (Data_Set_Ready    ) *    32
      ;      (Ring_Indicator    ) *    64
      ;      (Carrier_Detect    ) *   128
      ;      ----------------------------
      ;      (Char  received    ) *   256
      ;      (Char smothered    ) *   512
      ;      (Parity error      ) *  1024
      ;      (Framing error     ) *  2048
      ;      (Break detected    ) *  4096
      ;      (Able to xmit      ) *  8192
      ;      (Transmit idle     ) * 16384
      ;      (Time out error    ) * 32768
@@poll:
 mov bl, [byte di+7Ch]  ; Wait on bh in status or error

@@loop:
 sub cx, cx    ; Outer delay loop
@@loop_2:
 in al, dx    ;   inner loop
 mov ah, al
 and al, bh    ; And status with user bh mask
 cmp al, bh
 jz @@loop_exit   ;   jump if mask set
 loop @@loop_2   ; Else try again
 dec bl
 jnz @@loop
 or bh, bh    ; Clear mask to show timeout
@@loop_exit:
 retn     ; Exit ah reg. Z flag status

@@wait: add dx, 4    ; Reset the Modem Control Reg.
 out dx, al    ;   index RS232_BASE + 4
 inc dx    ; Calculate Modem Status Reg.
 inc dx    ;   index RS232_BASE + 6
 push bx    ; Save masks (bh=MSR, bl=LSR)
 call @@poll    ;   wait on MSR modem status
 pop bx    ;   restore wait masks bh, bl
 jnz @@done    ;   "Error Somewhere" by dec

 dec dx    ; Calculate Line Status Reg.
 mov bh, bl    ;   index RS232_BASE + 5
 call @@poll    ;   wait on LSR line status
@@done:
 retn     ; Status in ah reg and Z flag

endp int_14


str_no_basic db 'No ROM BASIC, booting from disk...', 0
str_serial db 195, ' Serial Port at ', 0


;---------------------------------------------------------------------------------------------------
; Interrupt 16h - Keyboard BIOS Services
;---------------------------------------------------------------------------------------------------
 entry 0E82Eh    ; IBM entry, key BIOS service
proc int_16 far

 sti     ; Keyboard BIOS services
 push ds
 push bx
 mov bx, 40h
 mov ds, bx    ; Load work segment
 or ah, ah
 jz @@read    ; Read keyboard buffer, ah=0
 dec ah
 jz @@check    ; Set Z if char ready, ah=1
 dec ah
 jz @@shift    ; Return shift in al, ah=2

@@end:
 pop bx    ; Exit INT_16 keyboard service
 pop ds
 iret

@@read:
 cli     ; No interrupts, alters buffer
 mov bx, [ds:1Ah]   ;   point to buffer head
 cmp bx, [ds:1Ch]   ; If not equal to buffer tail
 jnz @@have_char   ;   char waiting to be read
 sti     ; Else allow interrupts
 jmp @@read    ;   wait for him to type

@@have_char:
 mov ax, [bx]   ; Fetch the character
 inc bx    ;   point to next character
 inc bx    ;   char = scan code + shift
 mov [ds:1Ah], bx   ; Save position in head
 cmp bx, [ds:82h]   ;   buffer overflowed?
 jnz @@end    ;   no, done
 mov bx, [ds:80h]   ; Else reset to point at start
 mov [ds:1Ah], bx   ;   and correct head position
 jmp @@end

@@check:
 cli     ; No interrupts, critical code
 mov bx, [ds:1Ah]   ;   point to buffer head
 cmp bx, [ds:1Ch]   ;   equal buffer tail?
 mov ax, [bx]   ;   (fetch, look ahead)
 sti     ; Enable interrupts
 pop bx
 pop ds
 retf 2    ; Do iret, preserve flags

@@shift:
 mov al, [ds:17h]   ; Read keypad shift status
 jmp @@end

endp int_16


;---------------------------------------------------------------------------------------------------
; Interrupt 9h - Keyboard Data Ready
;---------------------------------------------------------------------------------------------------
 entry 0E885h    ; Align INT_9 at correct place

ascii  db 000h, 037h, 02Eh, 020h ; Scan -> ASCII, sign bit set
  db 02Fh, 030h, 031h, 021h ;   if further work needed
  db 032h, 033h, 034h, 035h
  db 022h, 036h, 038h, 03Eh
  db 011h, 017h, 005h, 012h
  db 014h, 019h, 015h, 009h
  db 00Fh, 010h, 039h, 03Ah
  db 03Bh, 084h, 001h, 013h
  db 004h, 006h, 007h, 008h
  db 00Ah, 00Bh, 00Ch, 03Fh
  db 040h, 041h, 082h, 03Ch
  db 01Ah, 018h, 003h, 016h
  db 002h, 00Eh, 00Dh, 042h
  db 043h, 044h, 081h, 03Dh
  db 088h, 02Dh, 0C0h, 023h
  db 024h, 025h, 026h, 027h
  db 028h, 029h, 02Ah, 02Bh
  db 02Ch, 0A0h, 090h

non_alpha db 032h, 036h, 02Dh, 0BBh ; Non-Alphabetic secondary
  db 0BCh, 0BDh, 0BEh, 0BFh ;   translation table
  db 0C0h, 0C1h, 0C2h, 0C3h
  db 0C4h, 020h, 031h, 033h
  db 034h, 035h, 037h, 038h
  db 039h, 030h, 03Dh, 01Bh
  db 008h, 05Bh, 05Dh, 00Dh
  db 05Ch, 02Ah, 009h, 03Bh
  db 027h, 060h, 02Ch, 02Eh
  db 02Fh

ctrl_upper db 040h, 05Eh, 05Fh, 0D4h ; CTRL uppercase secondary
  db 0D5h, 0D6h, 0D7h, 0D8h ;   translation table
  db 0D9h, 0DAh, 0DBh, 0DCh ;   for non-ASCII control
  db 0DDh, 020h, 021h, 023h
  db 024h, 025h, 026h, 02Ah
  db 028h, 029h, 02Bh, 01Bh
  db 008h, 07Bh, 07Dh, 00Dh
  db 07Ch, 005h, 08Fh, 03Ah
  db 022h, 07Eh, 03Ch, 03Eh
  db 03Fh

ctrl_lower db 003h, 01Eh, 01Fh, 0DEh ; CTRL lowercase secondary
  db 0DFh, 0E0h, 0E1h, 0E2h ;   translation table
  db 0E3h, 0E4h, 0E5h, 0E6h ;   for non-ASCII control
  db 0E7h, 020h, 005h, 005h
  db 005h, 005h, 005h, 005h
  db 005h, 005h, 005h, 01Bh
  db 07Fh, 01Bh, 01Dh, 00Ah
  db 01Ch, 0F2h, 005h, 005h
  db 005h, 005h, 005h, 005h
  db 005h

alt_key  db 0F9h, 0FDh, 002h, 0E8h ; ALT key secondary
  db 0E9h, 0EAh, 0EBh, 0ECh ;   translation table
  db 0EDh, 0EEh, 0EFh, 0F0h
  db 0F1h, 020h, 0F8h, 0FAh
  db 0FBh, 0FCh, 0FEh, 0FFh
  db 000h, 001h, 003h, 005h
  db 005h, 005h, 005h, 005h
  db 005h, 005h, 005h, 005h
  db 005h, 005h, 005h, 005h
  db 005h

num_pad  db '789-456+1230.'  ; Keypad secondary tralsator

num_ctrl db 0F7h, 005h, 004h, 005h ; Numeric keypad CTRL secondary
  db 0F3h, 005h, 0F4h, 005h ;   translation table
  db 0F5h, 005h, 0F6h, 005h
  db 005h

num_upper db 0C7h, 0C8h, 0C9h, 02Dh ; Numeric keypad SHIFT secondary
  db 0CBh, 005h, 0CDh, 02Bh ;   translation table
  db 0CFh, 0D0h, 0D1h, 0D2h
  db 0D3h

proc int_9 far

 sti     ; Key press hardware interrupt
 push ax
 push bx
 push cx
 push dx
 push si
 push di
 push ds
 push es
 cld
 mov ax, 40h
 mov ds, ax
 in al, 60h    ; Read the scan code data
 push ax    ;   save it
 in al, 61h    ; Get control port status
 push ax    ;   save it
 or al, 10000000b   ; Set "latch" bit to
 out 61h, al    ;   acknowledge data
 pop ax    ; Restore control status
 out 61h, al    ;   to enable keyboard
 pop ax    ;   restore scan code

ifdef KEYB_SHIFT_FIX
 call shift_key_hack   ; Fix for SHIFT+CURSOR/NAV keys on 101-key
endif
 mov ah, al    ; Save copy of scan code
 cmp al, 11111111b   ;   check for overrun
 jnz @@process   ;   no, OK
 jmp @@beep    ; Else beep bell on overrun

@@end: mov al, 20h    ; Send end_of_interrupt code
 out 20h, al    ;   to 8259 interrupt chip

@@exit:
 pop es    ; Exit the interrupt
 pop ds
 pop di
 pop si
 pop dx
 pop cx
 pop bx
 pop ax
 iret

@@process:
 and al, 01111111b   ; Valid scan code, no break
 cmp al, 46h
 jbe @@standard   ; Standard key
 jmp @@pad    ; Numeric keypad key

@@standard:
 mov bx, offset ascii  ; Table for ESC thru Scroll Lck
 xlat [cs:bx]    ;   translate to Ascii
 or al, al    ; Sign flags "Shift" type key
 js @@flag    ;   shift, caps, num, scroll etc
 or ah, ah    ; Invalid scan code?
 js @@end    ;   exit if so
 jmp short @@ascii   ; Else normal character

@@flag: and al, 01111111b   ; Remove sign flag bit
 or ah, ah    ;   check scan code
 js @@shift_up   ;   negative, key released
 cmp al, 10h    ; Is it a "toggle" type key?
 jnb @@toggle   ;   yes
 or [ds:17h], al   ; Else set bit in "flag" byte
 jmp @@end    ;   and exit

@@toggle:
 test [byte ds:17h], 00000100b ; Control key pressed?
 jnz @@ascii    ;   yes, skip
 test al, [ds:18h]   ; Else check "CAPS, NUM, SCRL"
 jnz @@end    ;   set, invalid, exit
 or [ds:18h], al   ; Show set in "flag_1" byte
 xor [ds:17h], al   ;   flip bits in "flag" byte
 jmp @@end

@@shift_up:
 cmp al, 10h    ; Released - is it "toggle" key
 jnb @@toggle_up   ;   skip if so
 not al    ; Else form two's complement
 and [ds:17h], al   ;   to do BIT_CLEAR "flags"
 cmp al, 11110111b   ; ALT key release special case
 jnz @@end    ;   no, exit
 mov al, [ds:19h]   ; Else get ALT-keypad character
 mov ah, 0    ;   pretend null scan code
 mov [ds:19h], ah   ;   zero ALT-keypad character
 cmp al, ah    ; Was there a valid ALT-keypad?
 jz @@end    ;   no, ignore, exit
 jmp @@null    ; Else stuff it in ASCII buffer

@@toggle_up:
 not al    ; Form complement of toggle key
 and [ds:18h], al   ;   to do BIT_CLEAR "flag_1"
 jmp @@end

@@ascii:
 test [byte ds:18h], 00001000b ; Scroll lock pressed?
 jz @@no_lock   ;   no
 cmp ah, 45h    ; Is this a NUM LOCK character?
 jz @@done    ;   no
 and [byte ds:18h], 11110111b ; Else clear bits in "flag_1"

@@done: jmp @@end    ;   and exit

@@no_lock:
 test [byte ds:17h], 00001000b ; ALT key pressed?
 jnz @@alt    ;   yes
 test [byte ds:17h], 00000100b ; CTRL key pressed?
 jnz @@ctrl    ;   yes
 test [byte ds:17h], 00000011b ; Either shift key pressed?
 jnz @@shift    ;   yes

@@lower_case:
 cmp al, 1Ah    ; Alphabetic character?
 ja @@non_alpha   ;   no
 add al, 'a'-1   ; Else add lower case base
 jmp @@common

@@non_alpha:
 mov bx, offset non_alpha  ; Non-alphabetic character
 sub al, 20h
 xlat [cs:bx]    ;   do the xlate
 jmp @@common

@@alt: cmp al, 1Ah    ; Control key pressed?
 ja @@no_ctrl   ;   no, skip
 mov al, 0    ; Else illegal key press
 jmp @@buffer

@@no_ctrl:
 mov bx, offset alt_key  ; Load ALT key translation
 sub al, 20h    ;   bias to printing char
 xlat [cs:bx]    ;   do the translation
 jmp @@common

@@ctrl: cmp ah, 46h    ; Scroll lock key?
 jnz @@ctrl_1   ;   no, skip
 mov [byte ds:71h], 10000000b ; Else CTRL-"Scroll" = break
 mov ax, [ds:80h]   ;   get key buffer start
 mov [ds:1Ch], ax   ;   get key tail to start
 mov [ds:1Ah], ax   ;   get key head to start
 int 1Bh    ; Issue a "Break" interrupt
 sub ax, ax
 jmp @@common_2

@@ctrl_1:
 cmp ah, 45h    ; Num lock key?
 jnz @@ctrl_2   ;   no, skip
 or [byte ds:18h], 00001000b ; Else show scroll lock
 mov al, 20h    ;   send end_of_interrupt
 out 20h, al    ;   to 8259 int controller
 cmp [byte ds:49h], 7  ; Monochrome monitor?
 jz @@poll    ;   yes, skip
 mov dx, 3D8h   ; Else reset mode
 mov al, [ds:65h]   ;   for the
 out dx, al    ;   CGA color card

@@poll: test [byte ds:18h], 00001000b ; Wait for him to type
 jnz @@poll    ;   not yet
 jmp @@exit

@@ctrl_2:
 cmp ah, 3    ; Is it a Control @ (null) ?
 jnz @@ctrl_3   ;   no
 mov al, 0    ; Else force a null

@@ctrl_4:
 jmp @@buffer   ;   save in buffer

@@ctrl_3:
 cmp al, 1Ah    ; Is it a control character?
 jbe @@ctrl_4   ;   yes
 mov bx, offset ctrl_lower  ; Else non-ascii control
 sub al, 20h    ;   lower case
 xlat [cs:bx]    ;   translation
 jmp @@common

@@shift:
 cmp ah, 37h    ; Print_Screen pressed?
 jnz @@shift_2
 mov al, 20h    ; Yes, send end_of_interrupt
 out 20h, al    ;   to 8259 interrupt chip
 int 5    ; Request print_screen service
 jmp @@exit    ;   and exit key service

@@shift_2:
 cmp al, 1Ah    ; Alphabetic char?
 ja @@shift_3   ;   no
 add al, 'A'-1   ; Yes, add base for alphabet
 jmp @@common

@@shift_3:
 mov bx, offset ctrl_upper  ; Non-ascii control
 sub al, 20h    ;   upper case
 xlat [cs:bx]    ;   translation
 jmp @@common

@@pad:
 sub al, 47h    ; Keypad key, convert origin
 mov bl, [ds:17h]   ;   get "flag" byte
 test bl, 00001000b   ; Look for ALT keypad entry
 jnz @@alt_num   ;   do special entry thing
 test bl, 00000100b   ; CTRL key pressed?
 jnz @@released   ;   skip if so
 test bl, 00100000b   ; Toggle "Num Lock" ?
 jz @@pad_1    ;   no, continue
 test bl, 00000011b   ; Shift keys hit?
 jnz @@pad_2    ;   no, check "INS"
 jmp short @@pad_5   ; Else xlat keypad char.

@@pad_1:
 test bl, 00000011b   ; Shift keys hit?
 jz @@pad_2    ;   no, check "INS" key
 jmp short @@pad_5   ; Else xlat keypad char.

@@alt_num:
 or ah, ah    ; ALT-keypad entry, scan code
 js @@done_2   ;   out of range
 test [byte ds:17h], 00000100b ; Else check CTRL state
 jz @@alt_num_2   ;   not pressed, ALT keypad

@@turbo_patch:
 cmp ah, 53h    ; Patch for CTRL ALT - toggle
 jnz @@turbo_check   ;   not a DEL (reset)
 mov [word ds:72h], 1234h  ; Ctrl-Alt-Del, set init flag
 jmp warm_boot   ;   do a warm reboot

@@turbo_check:
 cmp ah, 4Ah    ; Is it a keypad "-" ?
 jnz @@alt_num_2   ;   no, skip
 call toggle_turbo

@@alt_num_2:
 mov bx, offset num_pad  ; Get keypad translation table
 xlat [cs:bx]    ;   convert to number
 cmp al, '0'    ; Is it a valid ASCII digit?
 jb @@done_2   ;   no, ignore it
 sub al, 30h    ; Else convert to number
 mov bl, al    ;   save a copy
 mov al, [ds:19h]   ; Get partial ALT-keypad sum
 mov ah, 0Ah    ;   times 10 (decimal)
 mul ah
 add al, bl    ; Add in new digit to sum
 mov [ds:19h], al   ;   save as new ALT entry

@@done_2:
 jmp @@end    ; End_of_interrupt, exit

@@released:
 or ah, ah    ; Key released?
 js @@done_2   ;   ignore if so
 mov bx, offset num_ctrl  ; Else Numeric Keypad Control
 xlat [cs:bx]    ;   secondary translate
 jmp short @@common   ;   and save it

@@pad_2:
 cmp ah, 0D2h   ; Was "INS" key released?
 jnz @@pad_3
 and [byte ds:18h], 01111111b ; Yes, clear "INS" in "FLAG_1"
 jmp @@done_2

@@pad_3:
 or ah, ah    ; Key released?
 js @@done_2   ;   ignore if so
 cmp ah, 52h    ; Else check for "INS" press
 jnz @@pad_4    ;   not "INS" press
 test [byte ds:18h], 10000000b ; Was INS key in effect?
 jnz @@done_2   ;   yes, ignore
 xor [byte ds:17h], 10000000b ; Else tog "INS" in "FLAG" byte
 or [byte ds:18h], 10000000b ;   set "INS" in "FLAG_1" byte

@@pad_4: mov bx, offset num_upper  ; Numeric Keypad Upper Case
 xlat [cs:bx]    ;   secondary translation
 jmp short @@common

@@pad_5:
 or ah, ah    ; Was the key released?
 js @@done_2   ;   yes, ignore
 mov bx, offset num_pad  ; Load translation table
 xlat [cs:bx]    ;   do translate
 jmp short @@common

@@common:
 cmp al, 5    ; Common entry, char in al
 jz @@done_3   ;   Control E, ignore
 cmp al, 4
 ja @@common_1   ; Above Control D

 or al, 10000000b   ; Else set sign flag
 jmp short @@common_2

@@common_1:
 test al, 10000000b   ; Is sign bit set?
 jz @@common_3   ;   skip if so
 and al, 01111111b   ; Else mask sign off

@@common_2:
 mov ah, al    ; Save in high order byte
 mov al, 0    ;   set scan code to zero

@@common_3:
 test [byte ds:17h], 01000000b ; Test for "CAPS LOCK" state
 jz @@buffer   ;   no, skip
 test [byte ds:17h], 00000011b ; Test for SHIFT key
 jz @@common_4   ;   skip if no shift
 cmp al, 'A'    ; Check for alphabetic key
 jb @@buffer   ;   not SHIFT_able
 cmp al, 'Z'    ; Check for alphabetic key
 ja @@buffer   ;   not SHIFT_able
 add al, 20h    ; Else do the shift
 jmp short @@buffer

@@common_4:
 cmp al, 'a'    ; Check for alphabetic key
 jb @@buffer   ;   not SHIFT_able
 cmp al, 'z'    ; Check for Alphabetic key
 ja @@buffer   ;   not SHIFT_able
 sub al, 20h    ; Else do the shift

@@buffer:
 mov bx, [ds:1Ch]   ; bx = tail of buffer
 mov di, bx    ;   save it
 inc bx    ;   advance
 inc bx    ;   by word
 cmp bx, [ds:82h]   ; End of buffer reached?
 jnz @@check    ;   no, skip
 mov bx, [ds:80h]   ; Else bx = beginning of buffer

@@check:
 cmp bx, [ds:1Ah]   ; bx = Buffer Head ?
 jnz @@stuff    ;   no, OK
 jmp short @@beep   ; Else buffer overrun, beep

@@stuff:
 mov [di], ax   ; Stuff scan code, char in buffer
 mov [ds:1Ch], bx   ;   and update buffer tail

@@done_3:
 jmp @@end

@@beep: mov al, 20h    ; Keyboard beeper routine
 out 20h, al    ;   send end_of_interrupt
 mov bx, 80h    ; Cycles in beep
 in al, 61h    ;   get status
 push ax    ;   save copy
@@cycle:
 and al, 11111100b   ; Mask off speaker bits
 out 61h, al    ;   disable speaker
@@wave:
 mov cx, 64h    ; Constant for pitch
@@delay:
 loop @@delay    ;   delay, speaker off
 xor al, 00000010b
 out 61h, al    ; Toggle speaker position
 test al, 00000010b   ; Full cycle done yet?
 jz @@wave    ;   no, do other half cycle
 dec bx    ; Else show cycle sent
 jnz @@cycle    ;   more cycles to send
 pop ax
 out 61h, al    ; Restore flags
 mov cx, 32h    ; Silence counter
@@delay_end:
 loop @@delay_end   ; Send nothing for while
 jmp @@end

@@null: mov ah, 38h    ; ALT key pressed, released
 jmp @@buffer   ;   for no logical reason

endp int_9


;---------------------------------------------------------------------------------------------------
; Toggles TURBO speed and sounds high/low blip on the PC speaker to indicate current speed
;---------------------------------------------------------------------------------------------------
proc toggle_turbo near

 push ax
 push bx
 push cx
 in al, 61h    ; Read equipment flags
 xor al, 00001100b   ;   toggle speed
 out 61h, al    ; Write new flags back

 mov bx, 0F89h   ; low pitch blip
 and al, 4    ; Is turbo mode set?
 jz @@do_beep
 mov bx, 52Eh   ; high pitch blip

@@do_beep:
 mov al, 10110110b   ; Timer IC 8253 square waves
 out 43h, al    ;   channel 2, speaker
 mov ax, bx
 out 42h, al    ;   send low order
 mov al, ah    ;   load high order
 out 42h, al    ;   send high order
 in al, 61h    ; Read IC 8255 machine status
 push ax
 or al, 00000011b
 out 61h, al    ; Turn speaker on
 mov cx, 2000h
@@delay:
 loop    @@delay
 pop ax
 out 61h, al    ; Turn speaker off
 pop cx
 pop bx
 pop ax
 ret

endp toggle_turbo


;---------------------------------------------------------------------------------------------------
; Corrects line drawing character displayed for last device on BIOS config screen
;---------------------------------------------------------------------------------------------------
proc fix_last_line_char near

 mov ax, bx
 call locate
 mov si, offset str_last_line
 call print
 ret

endp fix_last_line_char


;---------------------------------------------------------------------------------------------------
; Interrupt 13h - Floppy Disk Services
;---------------------------------------------------------------------------------------------------
 entry 0EC59h    ; IBM entry point for floppy
proc int_13 far

 sti     ; Floppy disk services
 push bp
 push si
 push di
 push ds
 push es
 push bx
 mov di, ax    ; Request type in di, for index
 xor ax, ax
 mov ds, ax
 les si, [dword ds:78h]  ; Get disk parameter table
 mov ax, 40h
 mov ds, ax
 mov bx, 5
 mov ax, [es:bx+si]   ; Get (Gap Length, DTL) in ax
 push ax    ;   save it
 dec bx
 dec bx
 mov ax, [es:bx+si]   ; Get (Bytes/sector, EOT) in ax
 push ax    ;   save it
 xchg cl, dh
 xchg dl, cl
 push dx    ; Push (Head, Drive) swapped
 push cx
 push di
 mov bp, sp    ; Mark bottom of stack frame

ifdef SLOW_FLOPPY  ; EC85
 call floppy_speed   ;   call floppyquest at low speed
else
 call floppy_exec   ;   execute at current speed
endif

 mov ah, [es:si+2]   ; Get new motor count
 mov [ds:40h], ah   ;   and save it
 mov ah, [ds:41h]   ; Get completion status
 cmp ah, 1    ;   check for write protect
 cmc     ;   was write protect error
 pop bx
 pop cx
 pop dx
 xchg dl, cl
 xchg cl, dh
 pop bx    ; Clean
 pop bx    ;   up
 pop bx    ;   stack
 pop es
 pop ds
 pop di
 pop si
 pop bp
 retf 2

floppy_exec:
 mov al, [bp+1]   ; Get floppy service number
 or al, al
 jz @@reset    ;   reset, ah=0
 dec al
 jz @@status   ;   read status, ah=1
 cmp [byte bp+2], 3   ; For track number above 3?
 ja @@error    ;   yes
 cmp al, 5    ; Service within range?
 jbe @@exec    ;   yes

@@error:
 mov [byte ds:41h], 1  ; Say write protect error
 retn

@@exec: jmp @@service   ; Execute legal service

@@status:
 mov al, [ds:41h]   ; Return NEC status byte
 retn

@@reset:
 mov dx, 3F2h   ; Reset the floppy disk system
 cli
 and [byte ds:3Fh], 00001111b ; Clear "write in progress"
 mov al, [ds:3Fh]   ;   find out busy drives
 mov cl, 4
 shl al, cl
 test al, 00100000b
 jnz @@reset_1   ; Drive #1 active
 test al, 01000000b
 jnz @@reset_2   ; Drive #2 active
 test al, 10000000b
 jz @@reset_0   ; Drive #3 idle

@@reset_3:
 inc al
@@reset_2:
 inc al
@@reset_1:
 inc al
@@reset_0:
 mov [byte ds:3Eh], 0  ; All drives need recalibrate
 mov [byte ds:41h], 0  ;   no completion status
 or al, 00001000b   ; Interrupt ON in command word
 out dx, al    ;   send word to controller
 or al, 00000100b   ; "Reset" in command word
 out dx, al    ;   send word to controller
 sti
 call @@nec_busy   ; Wait for completion
 call nec_status   ;   read result block
 mov al, [ds:42h]
 cmp al, 0C0h   ; Did the reset work
 jz @@reset_ok   ;   yes
 mov [byte ds:41h], 20h  ; Else set controller error
 jmp short @@done   ;   return

@@reset_ok:
 mov al, 3    ; Specify command to NEC
 call nec_chip   ;   send it
 mov al, [es:si]   ; First byte in param block
 call nec_chip   ;   send it
 mov al, [es:si+1]   ; Second byte in param block
 call nec_chip   ;   send it

@@done: retn

func_table db 003h, 000h, 0E6h, 0C5h, 0E6h, 04Dh ; NEC function table lookup
dma_table db 000h, 000h, 046h, 04Ah, 042h, 04Ah ; DMA modes for 8237
write_table db 000h, 000h, 000h, 080h, 000h, 080h ; Write flag table lookup
drive_table db 1, 2, 4, 8    ; Drive number table lookup
error_table db 80h, 20h, 10h, 4, 2, 1   ; Error code table lookup
status_table db 04h, 10h, 08h, 04h, 03h, 02h, 20h ; Disk status table lookup

@@service:
 cli     ; Normal (non-reset) commands
 mov [byte ds:41h], 0  ;   reset status
 mov al, [bp+1]   ; Get command word
 mov ah, 0
 mov di, ax    ; Save copy, zero-extended
 out 0Ch, al    ;   diddle LSB/MSB flip-flop
 mov al, [cs:di+dma_table]  ; Fetch DMA mode
 out 0Bh, al    ;   send it to init_dma_controller
 mov ax, [bp+0Ch]   ; Get segment address
 mov cl, 4    ;   convert
 rol ax, cl    ;   to (offset, 64K page number)
 mov ch, al    ; Extract page number (0-15)
 and ch, 00001111b   ;   for 8237 DMA controller
 and al, 11110000b   ; Extract implicit page offset
 add ax, [bp+0Ah]   ;   add explicit user offset
 adc ch, 0    ;   (page number overflowed)
 mov dx, ax    ; Now save low 16 bits of address
 out 4, al    ;   send lowest 8 bits  " "
 mov al, ah
 out 4, al    ;   send next 8 bits  " "
 mov al, ch
 out 81h, al    ; 64K page number to DMA page reg
 mov ah, [bp+0]
 mov al, 0
 shr ax, 1    ; Sector count * 128
 mov cl, [bp+6]   ; Track count
 shl ax, cl    ;  * sector count
 dec ax    ; - 1
 out 5, al    ; Send 1/2 of the word count
 xchg al, ah
 out 5, al    ; Send 2/2 of the word count
 xchg al, ah
 add ax, dx    ; Compute final address
 jnb @@disable_dma   ;   ok
 sti
 mov [byte ds:41h], 9h  ; Else wrapped around 64K byte
 jmp @@overflow   ;   page register

@@disable_dma:
 mov al, 2    ; Disable floppy disk dma
 out 0Ah, al
 mov [byte ds:40h], 0FFh  ; Set large motor timeout
 mov bl, [bp+2]   ;   get drive number
 mov bh, 0
 mov al, [cs:bx+drive_table]  ; Table lookup bit position
 mov ch, al    ;   save mask
 mov cl, 4
 shl al, cl    ; Shift mask into place
 or al, bl    ;   or in drive select
 or al, 0Ch    ;   or in DMA and NO RESET
 mov dx, 3F2h
 out dx, al    ; Send to floppy control port
 sti
 mov al, [cs:di+write_table]  ; Table lookup for write flag
 or [ds:3Fh], al   ;   set write flag if active
 or al, al
 jns @@check_calibration  ;   skip if non-write
 mov ah, [es:si+0Ah]   ; Motor start from param blk
 or ah, ah
 jz @@check_calibration  ;   none specified
 test ch, [ds:3Fh]   ; Was this drive motor running?
 jnz @@check_calibration  ;   skip if so
 call @@wait    ; Else delay for motor start

@@check_calibration:
 or [ds:3Fh], ch   ; Show this motor is running
 test ch, [ds:3Eh]   ; Drive recalibration needed?
 jnz @@seek    ;   no, skip
 or [ds:3Eh], ch   ; Else show recalibrated
 mov al, 7    ; Send RECAL command
 call nec_chip   ;   to NEC 765 chip
 mov al, bl
 call nec_chip   ;   drive number
 call @@nec_busy   ; Wait for completion of RECAL
 call nec_return   ;   dummy call to ret

@@seek: mov al, 0Fh    ; Request a seek
 call nec_chip   ;   from the NEC 765
 mov al, bl
 call nec_chip   ; Drive number
 mov al, [bp+3]
 call nec_chip   ; Cylinder number
 call @@nec_busy   ;   wait for completion
 call nec_status   ;   read results
 mov al, [es:si+9]   ; Get head settle time
 or al, al    ;   none specified?
 jz @@translate   ;   if none, skip

@@head_settle:
 mov cx, 226h   ; Delay time for head settle
@@head_delay:
 loop @@head_delay   ;   timed wait
 dec al    ;   delay in millisec
 jnz @@head_settle   ;   wait some more

@@translate:
 mov al, [cs:di+func_table]  ; Translate user service, then
 call nec_chip   ;   and send as NEC func
 mov al, [bp+4]   ;
 and al, 1
 shl al, 1
 shl al, 1
 or al, bl
 call nec_chip
 cmp [byte bp+1], 5   ; Is this a format request?
 jnz @@not_format   ;   skip if not
 mov al, [bp+6]   ; Else use user bytes/sector
 call nec_chip
 mov al, [bp+7]   ;   user EOT
 call nec_chip
 mov al, [es:si+7]   ; Disk table format gap length
 call nec_chip
 mov al, [es:si+8]   ; Disk table format fill byte
 call nec_chip
 jmp short @@completion

@@not_format:
 mov cx, 7    ; Else lookup bytes * 512/sec
 mov di, 3    ;   from disk table

@@table_loop:
 mov al, [bp+di]   ; al has bytes/sector * 512
 call nec_chip
 inc di    ;   get next item for table
 loop @@table_loop   ;   also (EOT, GAP, DTL...)

@@completion:
 call @@nec_busy   ; Wait on floppy I/O completion
 call nec_status_alt   ;   get NEC status
 mov al, [ds:42h]   ;   into al
 and al, 11000000b   ; Isolate errors
 jz @@read_done   ;   no errors
 cmp al, 40h    ; Test direction bit
 jz @@error_code
 mov [byte ds:41h], 20h  ; Set if bad controller
 jmp short @@read_done  ;   return error

@@error_code:
 mov al, [ds:43h]   ; Read return code from block
 mov cx, 6    ;   number of error types
 xor bx, bx    ; Start at error type 0

@@error_loop:
 test al, [cs:bx+error_table]  ; Has error type bx occured?
 jnz @@xlat_error   ;   yes
 inc bx    ; Else try next error type
 loop @@error_loop   ;   until done

@@xlat_error:
 mov al, [cs:bx+status_table] ; Translate error code again
 mov [ds:41h], al   ;   store it as disk status

@@read_done:
 mov al, [ds:45h]   ; Get bytes read
 cmp al, [bp+3]   ;   compare with requested
 mov al, [ds:47h]   ; Read sectors requested
 jz @@skip    ;   return if all read
 mov al, [bp+7]   ; Else read sectors requested
 inc al    ;   add one for luck

@@skip: sub al, [bp+5]   ; Subtract stectors read
 retn

@@overflow:
 mov al, 0    ; Overflowed 64K page boundary
 retn     ;   show no sectors read

@@nec_busy:
 sti     ; Wait for operation to finish
 xor cx, cx    ;   zero low order delay
 mov al, 2    ; Load high order delay

@@busy_wait:
 test [byte ds:3Eh], 10000000b ; Has interrupt set the flag?
 clc     ;   hack to slow CPU
 jnz @@complete   ;   yes
 loop @@busy_wait   ; Else back for more
 dec al
 jnz @@busy_wait

 mov [byte ds:41h], 80h  ; Time-out, say it completed
 pop ax
 mov al, 0    ;   return time out code
 stc     ;   error status
 retn

@@complete:
 and [byte ds:3Eh], 01111111b ; Mask off completion status
 retn     ;   return carry clear

nec_ready:
 push cx    ; Wait for NEC ready for comand
 xor cx, cx
 mov dx, 3F4h   ;   NEC status port

@@ready_wait:
 in al, dx    ; Read status of NEC 765 chip
 or al, al
 js @@ready_ok   ;   able to accept command
 loop @@ready_wait
 mov [byte ds:41h], 80h  ; Else show timeout error
 jmp short @@ready_error

@@ready_ok:
 test al, 01000000b   ; Test the direction bit
 jnz @@ready_load
 mov [byte ds:41h], 20h  ;   clear if controller error

@@ready_error:
 pop cx
 stc
 retn

@@ready_load:
 inc dx    ; Load NEC data port
 in al, dx    ;   read it
 push ax

 mov cx, 0Ah    ; Short delay
@@ready_delay:
 loop @@ready_delay

 dec dx    ; Load NEC status port
 in al, dx    ;   read status
 test al, 00010000b   ;   set Z flag if done
 clc     ;   return success
 pop ax
 pop cx
 retn

@@wait: push cx    ; Millisecond delay in ah
@@delay:
 xor cx, cx
@@loop: loop @@loop
 dec ah
 jnz @@delay
 pop cx
 retn

endp int_13


;---------------------------------------------------------------------------------------------------
; Hack to allow SHIFT + Cursor/Nav key combinations on 101-key keyboards. The XT keyboard service
; does not interpret extended scan codes, so without this fix some extended key combinations do not
; work correctly due to the dummy shift codes. Uses 1 byte at 40:12 (test) and 2 bytes at 40:0E
; (LPT4 address) in the BDA for scratch space.
;---------------------------------------------------------------------------------------------------
ifdef KEYB_SHIFT_FIX
proc shift_key_hack near

 cmp al, 0E0h   ; Check for extended scan code
 jne @@check_shift   ; Not extended, so check for shift up scancode

@@extended_code:
 mov bl, 00000010b   ; Assume right shift down
 mov cl, [ds:12h]   ; Check for stored shift up
 cmp cl, 0AAh   ; Was it right shift?
 je @@was_shift   ; Yes
 cmp cl, 0B6h   ; Was it left shift?
 jne @@done    ; No, so we're done
 mov bl, 00000001b   ; Otherwise use left shift down

@@was_shift:
 mov cx, [ds:6Ch]   ; Check if timestamps match
 cmp cx, [ds:0Eh]   ; If they do, then fake shift was just sent
 jne @@done    ; Otherwise abort

@@shift_it:
 mov dl, [ds:17h]   ; Get current shift flags
 or dl, bl    ; Set left/right shift down
 mov [ds:17h], dl   ; Write shift flags back
 mov [ds:12h], al   ; Clear out old shift scancode
 ret

@@check_shift:
 cmp al, 0AAh   ; Is it a shift up scancode?
 je @@save    ; Yes, right
 cmp al, 0B6h   ; Check for left shift
 jne @@done    ; Not extended or shift up, so ignore it

@@save:
 mov [ds:12h], al   ; Record which shift up
 mov bx, [ds:6Ch]
 mov [ds:0Eh], bx   ; And save timestamp

@@done:
 ret

endp shift_key_hack
endif


;---------------------------------------------------------------------------------------------------
; Interrupt Eh - Diskette Controller
;---------------------------------------------------------------------------------------------------
 entry 0EF57h    ; Disk interrupt entry
proc int_E far

 sti     ; Floppy disk attention
 push ds
 push ax
 mov ax, 40h
 mov ds, ax
 or [byte ds:3Eh], 10000000b ; Raise "attention" flag
 mov al, 20h    ; Send end_of_interrupt code
 out 20h, al    ;   to 8259 interrupt chip
 pop ax
 pop ds
 iret

nec_status:
 mov al, 8    ; Send a "Request status"
 call nec_chip   ;   to the NEC 765 chip

nec_status_alt:
 push bx    ; Alternate entry point
 push cx
 mov cx, 7
 xor bx, bx

@@status_wait:
 call nec_ready   ; Wait for NEC 765 ready
 jb @@status_error   ;   NEC 765 error
 mov [bx+42h], al   ; Save status in BIOS block
 jz @@status_ok   ;   NEC 765 ready
 inc bx    ; Count more
 loop @@status_wait
 mov [byte ds:41h], 20h  ; NEC 765 controller error

@@status_error:
 stc     ; Set error condition
 pop cx
 pop bx
 pop ax
 mov al, 0
 retn

@@status_ok:
 pop cx    ; Successful return
 pop bx
 retn

nec_chip:
 push cx    ; Send control to NEC 765 chip
 push dx
 push ax
 xor cx, cx
 mov dx, 3F4h   ; Load NEC 765 status port

@@nec_wait:
 in al, dx    ; Read NEC 765 status
 or al, al
 js @@nec_ready   ;   done
 loop @@nec_wait
 mov [byte ds:41h], 80h  ; Set time out status
 jmp short @@nec_error

@@nec_ready:
 test al, 40h    ; Check data direction
 jz @@nec_data
 mov [byte ds:41h], 20h  ;   NEC 765 is gimped
 jmp short @@nec_error

@@nec_data:
 inc dx    ; Load NEC 765 data port
 pop ax
 out dx, al    ;   write user's parameter
 clc
 pop dx
 pop cx
nec_return:
 retn

@@nec_error:
 pop ax    ; Common error return
 pop dx
 pop cx
 pop ax
 mov al, 0
 stc
 retn

endp int_E


;---------------------------------------------------------------------------------------------------
; Interrupt 1Eh - Diskette Parameter Table
;---------------------------------------------------------------------------------------------------
 entry 0EFC7h    ; IBM entry for disk param
proc int_1E far

 db 11001111b   ; Disk parameter table
 db 2
 db 25h
 db 2
 db 8
 db 2Ah
 db 0FFh
 db 50h
 db 0F6h
 db 19h
 db 4

endp int_1E


;---------------------------------------------------------------------------------------------------
; Interrupt 17h - Parallel LPT Services
;---------------------------------------------------------------------------------------------------
 entry 0EFD2h    ; IBM entry for parallel LPT
proc int_17 far

 sti     ; Parallel printer services
 push ds
 push bx
 push cx
 push dx
 mov bx, 40h
 mov ds, bx
 mov bx, dx    ; dx is printer index (0 - 3)
 shl bx, 1    ;   word index
 mov dx, [bx+8]   ; Load printer port
 or dx, dx
 jz @@end    ; Goes to black hole
 or ah, ah
 jz @@print    ; Function is print, ah=0
 dec ah
 jz @@init    ; Function is init, ah=1
 dec ah
 jz @@status   ; Get the status, ah=2

@@end: pop dx
 pop cx
 pop bx
 pop ds
 iret

@@print:
 out dx, al    ; Char --> data lines 0-7
 inc dx    ; Printer status port
 mov bh, [bx+78h]   ; Load time out parameter
 mov ah, al

@@retry:
 xor cx, cx    ; Clear low order time out

@@poll: in al, dx    ; Get line printer status
 or al, al    ;   ready?
 js @@ready    ;   done if so
 loop @@poll
 dec bh    ; Decrement hi order time out
 jnz @@retry

 or al, 00000001b   ; Set timeout in Status Byte
 and al, 11111001b   ;   bits returned to caller
 jmp short @@toggle

@@ready:
 inc dx    ; Printer control port
 mov al, 00001101b   ; Set output strobe hi
 out dx, al    ;   data lines 0-7 valid

@@strobe:
 mov al, 00001100b   ; Set output strobe lo
 out dx, al    ;   data lines 0-7 ?????
 dec dx    ; Printer status port
 jmp short @@get_status  ;  get line printer status

@@status:
 mov ah, al    ; Save copy of character
 inc dx    ; Printer status port

@@get_status:
 in al, dx    ; Read printer status
 and al, 11111000b   ;   bits returned to caller

@@toggle:
 xor al, 01001000b   ;   toggle ERROR, ACKNOWLEDGE
 xchg al, ah
 jmp @@end    ; Exit, ah=status, al=character

@@init: mov ah, al    ; Initialize the line printer
 inc dx
 inc dx
 mov al, 00001000b
 out dx, al    ; Request initialize
 mov cx, 5DCh   ;   delay
@@delay:
 loop @@delay
 jmp @@strobe   ; Strobe the line printer

endp int_17


str_cga  db 195, ' CGA Graphics', 0


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Video BIOS (Mono/CGA) Main Entry
;---------------------------------------------------------------------------------------------------
 entry 0F045h    ; IBM entry point for table

video_funcs dw int_10_func_0  ; Set mode
  dw int_10_func_1  ; Set cursor type
  dw int_10_func_2  ; Set cursor position
  dw int_10_func_3  ; Get cursor position
  dw int_10_func_4  ; Read light pen position
  dw int_10_func_5  ; Set active display page
  dw int_10_func_6_7  ; Scroll active page up
  dw int_10_func_6_7  ; Scroll active page down
  dw int_10_func_8_9_10 ; Read attribute/character
  dw int_10_func_8_9_10 ; Write attribute/character
  dw int_10_func_8_9_10 ; Write character only
  dw int_10_func_11  ; Set color
  dw int_10_func_12  ; Write pixel
  dw int_10_func_13  ; Read pixel
  dw int_10_func_14  ; Write teletype
  dw int_10_func_15  ; Return current video state

 entry 0F065h    ; IBM entry, video BIOS service
proc int_10 far

 sti     ; Video BIOS service ah=(0-15)
 cld     ;   strings auto-increment
 push bp
 push es
 push ds
 push si
 push di
 push dx
 push cx
 push bx
 push ax
 mov bx, 40h
 mov ds, bx
 mov bl, [ds:10h]   ; Get equipment byte
 and bl, 00110000b   ;   isolate video mode
 cmp bl, 00110000b   ; Check for monochrome card
 mov bx, 0B800h
 jnz @@dispatch   ;   not there, bx --> CGA
 mov bx, 0B000h   ; Else bx --> MONO

@@dispatch:
 push bx    ; Save video buffer address
 mov bp, sp    ;   start of stack frame
 call int_10_dispatch   ;   then do the function
 pop si
 pop ax
 pop bx
 pop cx
 pop dx
 pop di
 pop si
 pop ds
 pop es
 pop bp
 iret

map_byte:
 push dx    ; Multiply al by bx, cx --> buffer
 mov ah, 0
 mul bx    ; Position in ax
 pop dx
 mov cx, [bp+0]   ; cx --> video buffer
 retn

endp int_10

;---------------------------------------------------------------------------------------------------
; Interrupt 1Dh - Video Parameter Tables
;---------------------------------------------------------------------------------------------------
 entry 0F0A4h    ; IBM entry, SET_MODE tables
proc int_1D far

 db 38h, 28h, 2Dh, 0Ah, 1Fh, 6, 19h ; Init string for 40x25 color
 db 1Ch, 2, 7, 6, 7
 db 0, 0, 0, 0

 db 71h, 50h, 5Ah, 0Ah, 1Fh, 6, 19h ; Init string for 80x25 color
 db 1Ch, 2, 7, 6, 7
 db 0, 0, 0, 0

 db 38h, 28h, 2Dh, 0Ah, 7Fh, 6, 64h ; Init string for graphics
 db 70h, 2, 1, 6, 7
 db 0, 0, 0, 0

 db 61h, 50h, 52h, 0Fh, 19h, 6, 19h ; Init string for 80x25 b/w
 db 19h, 2, 0Dh, 0Bh, 0Ch
 db 0, 0, 0, 0

regen_len dw 0800h   ; Regen length, 40x25
  dw 1000h   ;         80x25
  dw 4000h   ;         graphics
  dw 4000h

max_cols db 28h, 28h, 50h, 50h, 28h, 28h, 50h, 50h ; Maximum columns
modes  db 2Ch, 28h, 2Dh, 29h, 2Ah, 2Eh, 1Eh, 29h ; Table of mode sets
mul_lookup db 00h, 00h, 10h, 10h, 20h, 20h, 20h, 30h ; Table lookup for multiply

endp int_1D


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function Dispatch
;---------------------------------------------------------------------------------------------------
proc    int_10_dispatch near

 cmp ah, 0Fh    ; Is ah a legal video command?
 jbe @@ok
 ret     ;   error return if not

@@ok: shl ah, 1    ; Make word value
 mov bl, ah    ;   then set up bx
 mov bh, 0
 jmp [word cs:bx+video_funcs] ;   vector to routines

endp int_10_dispatch


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 0: Set mode
;---------------------------------------------------------------------------------------------------
proc int_10_func_0 near					; F115

 mov al, [ds:10h]   ; Set mode of CRT
 mov dx, 3B4h   ;   mono port
 and al, 00110000b   ;   get display type
 cmp al, 00110000b   ;   equal if mono
 mov al, 1    ; Assume mono display
 mov bl, 7    ;   mode is 7
 jz @@reset    ;   Skip if mono, else CGA
 mov bl, [bp+2]   ; bl = mode number (user al)
 mov dl, 0D4h   ; 3D4 is CGA port
 dec al

@@reset: ' F12C
 mov [ds:63h], dx   ; Save current CRT display port
 add dl, 4
 out dx, al    ; Reset the video
 mov [ds:49h], bl   ;   save current CRT mode
 push ds
 xor ax, ax
 mov ds, ax
 les si, [dword ds:74h]  ; si --> INT_1D video parameters
 pop ds
 mov bh, 0
 push bx
 mov bl, [cs:bx+mul_lookup]  ; Get bl for index into INT_1D
 add si, bx
 mov cx, 10h    ; Sixteen values to send

@@loop: mov al, [es:si]   ; Value to send in si
 call send_ax    ;   send it
 inc ah    ;   bump count
 inc si    ;   point to next
 loop @@loop    ;   loop until done

 mov bx, [bp+0]   ; bx --> regen buffer					; F15A
 mov es, bx    ;   into es segment
 xor di, di
 call mode_check   ; Set flags to mode
 mov cx, 2000h   ;   assume CGA							; F164
 mov ax, 0    ;   and graphics
 jb @@fill    ;   do graphics fill
 jnz @@text    ;   Alphanumeric fill
 mov cx, 800h   ;   mono card
@@text: mov ax, 7*100h+' '   ; Word for text fill
@@fill: repz stosw    ;   fill regen buffer

 mov dx, [ds:63h]   ; Get the port
 add dl, 4
 pop bx
 mov al, [cs:bx+modes]  ; Load data to set for mode
 out dx, al    ;   and send it
 mov [ds:65h], al   ;   then save active data
 inc dx
 mov al, 30h    ; Assume not 640x200 b/w
 cmp bl, 6    ;   correct?
 jnz @@palette
 mov al, 3Fh    ; Palette for 640x200 b/w

@@palette:
 mov [ds:66h], al   ;   save palette
 out dx, al    ;   send palette
 xor ax, ax
 mov [ds:4Eh], ax   ; Start at beg. of 1st page
 mov [ds:62h], al   ;   active page=page 0
 mov cx, 8    ; Do 8 pages of cursor data
 mov di, 50h    ; Page cursor data at 40:50

@@cursor:
 mov [di], ax   ; Cursor at upper left of page
 inc di    ;   next page
 loop @@cursor

 mov al, [cs:bx+max_cols]  ; Get display width					; F1AD
 mov [ds:4Ah], ax   ;   save it
 and bl, 11111110b
 mov ax, [word cs:bx+regen_len] ; Get video regen length
 mov [ds:4Ch], ax   ;   save it
 ret								;	F1BB

endp int_10_func_0


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 1: Set cursor type
;---------------------------------------------------------------------------------------------------
proc int_10_func_1 near					; F1BC

 mov cx, [bp+6]   ; Set cursor type, from cx
 mov [ds:60h], cx   ;   save it
 mov ah, 0Ah    ; CRT index register 0Ah
 call out_6845   ;   send ch, cl to CRT register
 ret

endp int_10_func_1


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 2: Set cursor position
;---------------------------------------------------------------------------------------------------
proc int_10_func_2 near					; F1C9

 mov bl, [bp+5]   ; Set cursor position, page bh
 shl bl, 1    ;   (our bl)
 mov bh, 0
 mov ax, [bp+8]   ; Position in user dx (our ax)
 mov [bx+50h], ax   ;   remember cursor position
 jmp set_cursor   ;   set 6845 cursor hardware

endp int_10_func_2


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 3: Get cursor position
;---------------------------------------------------------------------------------------------------
proc int_10_func_3 near

 mov bl, [bp+5]   ; Get cursor position, page bh
 shl bl, 1
 mov bh, 0
 mov ax, [bx+50h]
 mov [bp+8], ax   ;   return position in user dx
 mov ax, [ds:60h]   ; Get cursor mode
 mov [bp+6], ax   ;   return in user cx
 ret

endp int_10_func_3


pen_offset: db 3, 3, 5, 5, 3, 3, 3, 4 ; Light pen offset table


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 4: Read light pen position
;---------------------------------------------------------------------------------------------------
proc int_10_func_4 near

 mov dx, [ds:63h]
 add dl, 6
 mov [byte bp+3], 0   ; ah=0, assume not triggered
 in al, dx
 test al, 00000100b
 jz @@reset    ; Skip, reset if pen not set
 test al, 00000010b
 jnz @@triggered   ; Skip if pen triggered
 ret     ;   return, do not reset

@@triggered:
 mov ah, 10h    ; Offset to pen port is 10h
 call pen_pos    ;   read into ch, cl
 mov bl, [ds:49h]   ; Get CRT mode data word
 mov cl, bl
 mov bh, 0
 mov bl, [byte cs:bx+pen_offset] ; Load offset for subtraction
 sub cx, bx
 jns @@mode    ;   did not overflow
 xor ax, ax    ; Else fudge a zero

@@mode: call mode_check   ; Set flags on display type
 jnb @@text    ;   text mode, skip
 mov ch, 28h
 div dl
 mov bl, ah
 mov bh, 0
 mov cl, 3
 shl bx, cl
 mov ch, al
 shl ch, 1
 mov dl, ah
 mov dh, al
 shr dh, 1
 shr dh, 1
 cmp [byte ds:49h], 6  ; Mode 640x200 b/w?
 jnz @@done    ;   no, skip
 shl dl, 1
 shl bx, 1
 jmp short @@done

@@text: div [byte ds:4Ah]   ; Divide by columns in screen
 xchg al, ah    ;   as this is text mode
 mov dx, ax
 mov cl, 3
 shl ah, cl
 mov ch, ah
 mov bl, al
 mov bh, 0
 shl bx, cl

@@done: mov [byte bp+3], 1   ; Return ah=1, light pen read
 mov [bp+8], dx   ;   row, column in user dx
 mov [bp+4], bx   ;   pixel column in user bx
 mov [bp+7], ch   ;   raster line in user ch

@@reset:
 mov dx, [ds:63h]   ; Get port of active CRT card
 add dx, 7
 out dx, al    ;   reset the light pen
 ret

endp int_10_func_4


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 5: Set active display page
;---------------------------------------------------------------------------------------------------
proc int_10_func_5 near

 mov al, [bp+2]   ; Set active display page to al
 mov [ds:62h], al   ;   save new active page
 mov ah, 0    ;   clear high order
 push ax
 mov bx, [ds:4Ch]   ; Get size of regen buffer
 mul bx    ;   times number of pages
 mov [ds:4Eh], ax   ; Now ax = CRT offset, save
 shr ax, 1    ;   now word offset
 mov cx, ax    ;   save a copy
 mov ah, 0Ch    ; CRT index register 0Ch
 call out_6845   ;   send ch, cl through CRT register
 pop bx
 call move_cursor   ; Save new parameters
 ret

endp int_10_func_5


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 6: Scroll active page up
;                 Function 7: Scroll active page down
;---------------------------------------------------------------------------------------------------
proc int_10_func_6_7 near

 call mode_check
 jnb @@text
 jmp @@graphics   ; Graphics scroll

@@text: cld     ; Strings go upward
 cmp [byte ds:49h], 2
 jb @@get_coords   ;   no retrace wait needed
 cmp [byte ds:49h], 3
 ja @@get_coords   ;   no retrace wait needed
 mov dx, 3DAh   ; Else 80x25, do the kludge

@@wait: in al, dx    ; Read CGA status register
 test al, 00001000b   ;   vertical retrace?
 jz @@wait    ;   wait until it is
 mov dx, 3D8h   ; Then go and
 mov al, 25h    ;   turn the display
 out dx, al    ;   off to avoid snow

@@get_coords:
 mov ax, [bp+8]   ; Get row, column of upper left
 push ax
 cmp [byte bp+3], 7   ; Check for scroll down
 jz @@offset   ;   yes, skip if so
 mov ax, [bp+6]   ; Get row, column of lower right

@@offset:
 call rc_to_col   ; Get byte offset in CRT buffer
 add ax, [ds:4Eh]   ;   add base for CRT buffer
 mov si, ax
 mov di, ax
 pop dx
 sub dx, [bp+6]   ; Subtract (row, col) lwr rhgt
 add dx, 101h   ;   width of one char
 mov bx, [ds:4Ah]   ; Get columns in display
 shl bx, 1    ;   bytes in row of display
 push ds
 mov al, [bp+2]   ; Get scroll fill character
 call map_byte   ;   calculate offset
 mov es, cx    ; cx --> byte in buffer
 mov ds, cx
 cmp [byte bp+3], 6   ; Scroll up?
 jz @@count    ;   skip if so
 neg ax
 neg bx
 std     ; Else start at top of page

@@count:
 mov cl, [bp+2]   ; Get count of lines to scroll
 or cl, cl
 jz @@attr    ;   nothing to do
 add si, ax
 sub dh, [bp+2]

@@scroll:
 mov ch, 0    ; Clear high order word count
 mov cl, dl    ;   load low order word count
 push di
 push si
 repz movsw    ; Do the scroll
 pop si
 pop di
 add si, bx    ; Move one line in direction
 add di, bx    ;  ""   ""
 dec dh    ; One less line to scroll
 jnz @@scroll
 mov dh, [bp+2]   ; Now get number of rows

@@attr: mov ch, 0    ; Clear high order word count
 mov ah, [bp+5]   ;   get fill attribute
 mov al, ' '    ;   fill character

@@fill: mov cl, dl    ; Get characters to scroll
 push di
 repz stosw    ;   store fill attr/char
 pop di
 add di, bx    ; Show row was filled
 dec dh
 jnz @@fill    ;   more rows are left
 pop ds
 call mode_check   ; Check for monochrome card
 jz @@done    ;   skip if so
 mov al, [ds:65h]   ; Get the mode data byte
 mov dx, 3D8h   ;   load active CRT card port
 out dx, al    ;   and unblank the screen

@@done: ret

@@graphics:
 cld     ; Assume graphics scroll up
 mov ax, [bp+8]   ; (Row, Col) of lower right
 push ax
 cmp [byte bp+3], 7   ; Scroll down?
 jz @@gfx_offset   ;   skip if so
 mov ax, [bp+6]   ; (Row, Col) of upper left

@@gfx_offset:
 call gfx_rc_col   ; Convert (Row, Col) -> Chars
 mov di, ax
 pop dx
 sub dx, [bp+6]   ; Chars to copy over
 add dx, 101h   ;   width of one char
 shl dh, 1
 shl dh, 1
 mov al, [bp+3]   ; Get command type
 cmp [byte ds:49h], 6  ;   is this 640x200?
 jz @@gfx_next   ;   skip if so
 shl dl, 1    ; Else bigger characters
 shl di, 1
 cmp al, 7    ; Is this scroll down?
 jnz @@gfx_next   ;   skip if not so
 inc di

@@gfx_next:
 cmp al, 7    ; Is this scroll down?
 jnz @@gfx_start   ;   skip if not so
 add di, 0F0h

@@gfx_start:
 mov bl, [bp+2]   ; Number of rows to blank
 shl bl, 1
 shl bl, 1
 push bx
 sub dh, bl    ; Subtract from row count
 mov al, 50h
 mul bl
 mov bx, 1FB0h
 cmp [byte bp+3], 6   ; Is this scroll up?
 jz @@gfx_end   ;   skip if so
 neg ax    ; Else do it
 mov bx, 2050h
 std     ;   in reverse

@@gfx_end:
 mov si, di    ; End of area
 add si, ax    ;   start
 pop ax
 or al, al
 mov cx, [bp+0]
 mov ds, cx
 mov es, cx
 jz @@gfx_attr   ; No rows to scroll
 push ax

@@gfx_scroll:
 mov ch, 0    ; Zero hi order byte count
 mov cl, dl    ;   bytes in row
 push si
 push di
 repz movsb    ; Copy one plane
 pop di
 pop si
 add si, 2000h   ; Load other graphics
 add di, 2000h   ;   video plane
 mov cl, dl
 push si
 push di
 repz movsb    ; Copy other plane
 pop di
 pop si
 sub si, bx
 sub di, bx
 dec dh    ; One less row to scroll
 jnz @@gfx_scroll   ;   loop if more to do
 pop ax
 mov dh, al    ; Load rows to blank

@@gfx_attr:
 mov al, [bp+5]   ; Get fill attribute
 mov ch, 0

@@gfx_fill:
 mov cl, dl    ; Get bytes per row
 push di
 repz stosb    ; Load row with fill attribute
 pop di
 add di, 2000h   ; Do other graphics video plane
 mov cl, dl
 push di
 repz stosb    ; Load row with fill attribute
 pop di
 sub di, bx
 dec dh    ; Show one less row to blank
 jnz @@gfx_fill   ;   loop if more to do
 ret

endp int_10_func_6_7


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 8: Read attribute/character
;                 Function 9: Write attribute/character
;                 Function 10: Write character only
;---------------------------------------------------------------------------------------------------
proc int_10_func_8_9_10 near

 call mode_check
 jb @@graphics   ; Graphics operation
 mov bl, [bp+5]   ; Get the display page
 mov bh, 0
 push bx
 call text_rc_col   ; Convert Row, Col, Page -> Col
 mov di, ax    ;   offset in di
 pop ax
 mul [word ds:4Ch]   ; Page length X page number
 add di, ax    ;   current char position
 mov si, di    ;   move into si
 mov dx, [ds:63h]   ; Display port into dx
 add dx, 6    ;   get status port
 push ds
 mov bx, [bp+0]   ; bx --> regen. buffer
 mov ds, bx
 mov es, bx
 mov al, [bp+3]   ; Get user (ah) function request
 cmp al, 8
 jnz @@write    ;   skip if not read attribute

@@read:
 in al, dx    ; Read CRT display status
 test al, 00000001b   ;   test for horizontal retrace
 jnz @@read    ; Yes, wait for display on
 cli     ;   no interrupts now

@@read_wait:
 in al, dx    ; Read CRT display status
 test al, 00000001b   ;   test for horizontal retrace
 jz @@read_wait   ;   not yet, wait for it

 lodsw     ; Read character/attribute
 pop ds
 mov [bp+2], al   ; Return character
 mov [bp+3], ah   ;   and attribute
 ret

@@write:
 mov bl, [bp+2]   ; Get character to write
 mov bh, [bp+4]   ;   attribute
 mov cx, [bp+6]   ;   character count
 cmp al, 0Ah    ; Write character only?
 jz @@char_write   ;   skip if so

@@write_loop:
 in al, dx    ; Read CRT display status
 test al, 00000001b   ;   test for horizontal retrace
 jnz @@write_loop   ; Yes, wait for display on
 cli     ;   no interrupts now

@@write_wait:
 in al, dx    ; Read CRT display status
 test al, 00000001b   ;   test for horizontal retrace
 jz @@write_wait   ;   not yet, wait for it

 mov ax, bx    ; Get char/attribute
 stosw     ;   write it
 loop @@write_loop   ;   loop for character count
 pop ds
 ret

@@char_write:
 in al, dx    ; Read CRT display status
 test al, 00000001b   ;   test for horizontal retrace
 jnz @@char_write   ;   not yet, wait for it
 cli     ;   no interrupts now

@@char_wait:
 in al, dx    ; Read CRT display status
 test al, 00000001b   ;   test for horizontal retrace
 jz @@char_wait   ;   not yet, wait for it

 mov al, bl    ; Get character
 stosb     ;   write it
 inc di    ;   skip attribute
 loop @@char_write   ;   loop for character count
 pop ds
 ret

@@graphics:
 cmp [byte bp+3], 8   ; Read graphics char/attr?
 jnz @@gfx_write   ;   no, must be write
 jmp @@gfx_read   ; Else read char/attr

@@gfx_write:
 mov ax, [ds:50h]   ; Get cursor position
 call gfx_rc_col   ;   convert (row, col) -> col
 mov di, ax    ; Save in displacement register
 push ds
 mov al, [bp+2]   ; Get character to write
 mov ah, 0
 or al, al    ; Is it user character set?
 js @@user_chars   ;   skip if so
 mov dx, cs    ; Else use ROM character set
 mov si, offset gfx_chars  ;   offset gfx_chars into si
 jmp short @@buffer

@@user_chars:
 and al, 7Fh    ; Origin to zero
 xor bx, bx    ;   then go load
 mov ds, bx    ;   user graphics
 lds si, [dword ds:7Ch]  ;   vector, offset in si
 mov dx, ds    ;   segment into dx

@@buffer:
 pop ds    ; Restore data segment
 mov cl, 3    ;   char 8 pixels wide
 shl ax, cl
 add si, ax    ; Add regen buffer base address
 mov ax, [bp+0]   ;   get regen buffer address
 mov es, ax    ;   into es
 mov cx, [bp+6]   ;   load character count
 cmp [byte ds:49h], 6  ; Is the mode 640x200 b/w?
 push ds
 mov ds, dx
 jz @@write_640x200   ;   skip if so
 shl di, 1
 mov al, [bp+4]   ; Get character attribute
 and ax, 3
 mov bx, 5555h
 mul bx
 mov dx, ax
 mov bl, [bp+4]

@@gfx_write_loop:
 mov bh, 8    ; Char 8 pixels wide
 push di
 push si

@@write_read:
 lodsb     ; Read the screen
 push cx
 push bx
 xor bx, bx
 mov cx, 8

@@shift:
 shr al, 1    ; Shift bits through byte
 rcr bx, 1
 sar bx, 1
 loop @@shift

 mov ax, bx    ; Result into ax
 pop bx
 pop cx
 and ax, dx
 xchg ah, al
 or bl, bl
 jns @@write_word
 xor ax, [es:di]

@@write_word:
 mov [es:di], ax   ; Write new word
 xor di, 2000h
 test di, 2000h   ; Is this other plane?
 jnz @@write_next   ;   nope
 add di, 50h    ; Else advance character

@@write_next:
 dec bh    ; Show another char written
 jnz @@write_read   ;   more to go
 pop si
 pop di
 inc di
 inc di
 loop @@gfx_write_loop
 pop ds
 ret

@@write_640x200:
 mov bl, [bp+4]   ; Get display page
 mov dx, 2000h   ;   size of graphics plane

@@write_loop_640:
 mov bh, 8    ; Pixel count to write
 push di
 push si

@@write_read_640:
 lodsb     ; Read from one plane
 or bl, bl    ;   done both planes?
 jns @@write_byte_640  ;   skip if not
 xor al, [es:di]   ; Else load attribute

@@write_byte_640:
 mov [es:di], al   ; Write out attribute
 xor di, dx    ;   get other plane
 test di, dx    ; Done both planes?
 jnz @@write_next_640  ;   skip if not
 add di, 50h    ; Else position for now char

@@write_next_640:
 dec bh    ; Show row of pixels read
 jnz @@write_read_640  ;   not done all of them
 pop si
 pop di
 inc di
 loop @@write_loop_640
 pop ds
 ret

@@gfx_read:
 cld     ; Increment upwards
 mov ax, [ds:50h]   ;   get cursor position
 call gfx_rc_col   ; Convert (row, col) -> columns
 mov si, ax    ;   save in si
 sub sp, 8    ; Grab 8 bytes temp storage
 mov di, sp    ;   save base in di
 cmp [byte ds:49h], 6  ; Mode 640x200 b/w?
 mov ax, [bp+0]   ;   ax --> CRT regen buffer
 push ds
 push di
 mov ds, ax
 jz @@640x200   ; Mode is 640x200 b/w - skip
 mov dh, 8    ; Eight pixels high/char
 shl si, 1
 mov bx, 2000h   ; Bytes per video plane

@@read_loop:
 mov ax, [si]   ; Read existing word
 xchg ah, al
 mov cx, 0C000h   ; Attributes to scan for
 mov dl, 0

@@attr: test ax, cx    ; Look for attributes
 clc
 jz @@skip    ;   set, skip
 stc     ; Else show not set

@@skip: rcl dl, 1
 shr cx, 1
 shr cx, 1
 jnb @@attr    ;   more shifts to go
 mov [ss:di], dl
 inc di
 xor si, bx    ; Do other video plane
 test si, bx    ;   done both planes?
 jnz @@row_done   ;   no, skip
 add si, 50h    ; Else advance pointer

@@row_done:
 dec dh    ; Show another pixel row done
 jnz @@read_loop   ;   more rows to do
 jmp short @@load_chars

@@640x200:
 mov dh, 4    ; Mode 640x200 b/w - special

@@read_plane:
 mov ah, [si]   ; Read pixels from one plane
 mov [ss:di], ah   ;   save on stack
 inc di    ;   advance
 mov ah, [si+2000h]   ; Read pixels from other plane
 mov [ss:di], ah   ; Save pixels on stack
 inc di    ;   advance
 add si, 50h    ; Total pixels in char
 dec dh    ;   another row processed
 jnz @@read_plane   ;   more to do

@@load_chars:
 mov dx, cs    ; Load segment of graphics char
 mov di, offset gfx_chars  ;   and offset
 mov es, dx    ;   save offset in es
 mov dx, ss
 mov ds, dx
 pop si
 mov al, 0

@@gfx_user_chars:
 mov dx, 80h    ; Number of characters in graphics set

@@gfx_read_loop:
 push si
 push di
 mov cx, 8    ; Bytes to compare for char
 repz cmpsb    ;   do compare
 pop di
 pop si
 jz @@read_done   ; Found graphics character
 inc al    ;   else show another char
 add di, 8    ;   advance one row
 dec dx    ;   one less char to scan
 jnz @@gfx_read_loop   ; Loop if more char left

 or al, al    ; User graphics character set?
 jz @@read_done   ;   no, not found
 xor bx, bx
 mov ds, bx
 les di, [dword ds:7Ch]  ; Else load user graphics char
 mov bx, es
 or bx, di
 jz @@read_done   ;   not found
 jmp @@gfx_user_chars  ; Try using user graphics char

@@read_done:
 mov [bp+2], al   ; Return char in user al
 pop ds
 add sp, 8    ;   return temp storage
 ret

endp int_10_func_8_9_10


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 11: Set color
;---------------------------------------------------------------------------------------------------
proc int_10_func_11 near

 mov dx, [ds:63h]   ; Set color, get CGA card port
 add dx, 5    ;   color select register
 mov al, [ds:66h]   ; Get CRT palette
 mov ah, [bp+5]   ;   new palette ID, user bh
 or ah, ah
 mov ah, [bp+4]   ;   new palette color, user bl
 jnz @@skip    ; Palette ID specified, skip
 and al, 0E0h
 and ah, 1Fh    ; Null ID = ID 01Fh
 or al, ah    ;   set in color
 jmp short @@new_palette

@@skip: and al, 0DFh
 test ah, 1
 jz @@new_palette
 or al, 20h

@@new_palette:
 mov [ds:66h], al   ; Save new palette
 out dx, al    ;   tell CGA about it
 ret

endp int_10_func_11


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 12: Write pixel
;---------------------------------------------------------------------------------------------------
proc int_10_func_12 near

 mov ax, [bp+0]   ; Write pixel
 mov es, ax
 mov dx, [bp+8]   ; Load row from user dx
 mov cx, [bp+6]   ;   col from user cx
 call dot_offset   ; Find dot offset
 jnz @@ok    ;   valid
 mov al, [bp+2]   ; Load user color
 mov bl, al
 and al, 1
 ror al, 1
 mov ah, 7Fh
 jmp short @@read

@@ok: shl cl, 1
 mov al, [bp+2]
 mov bl, al
 and al, 3
 ror al, 1
 ror al, 1
 mov ah, 3Fh

@@read: ror ah, cl
 shr al, cl
 mov cl, [es:si]   ; Read the char with the dot
 or bl, bl
 jns @@color
 xor cl, al    ; Exclusive or existing color
 jmp short @@write

@@color:
 and cl, ah    ; Set new color for dot
 or cl, al

@@write:
 mov [es:si], cl   ; Write out char with the dot
 ret

endp int_10_func_12


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 13: Read pixel
;---------------------------------------------------------------------------------------------------
proc int_10_func_13 near

 mov ax, [bp+0]   ; ax --> video regen buffer
 mov es, ax    ;   into es segment
 mov dx, [bp+8]   ; Load row from user dx
 mov cx, [bp+6]   ;   col from user cx
 call dot_offset   ; Calculate dot offset
 mov al, [es:si]   ;   read dot
 jnz @@offset   ;   was there
 shl al, cl
 rol al, 1
 and al, 1
 jmp short @@done

@@offset:
 shl cl, 1    ; Calculate offset in char
 shl al, cl
 rol al, 1
 rol al, 1
 and al, 3

@@done: mov [bp+2], al   ; Return dot pos in user al
 ret

endp int_10_func_13


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 14: Write teletype
;---------------------------------------------------------------------------------------------------
proc int_10_func_14 near

 mov bl, [ds:62h]   ; Get active video page (0-7)
 shl bl, 1    ;   as word index
 mov bh, 0    ;   clear high order
 mov dx, [bx+50h]   ; Index into cursor position

 mov al, [bp+2]   ; Get character to write
 cmp al, 8    ;   back space?
 jz @@back_space   ;   skip if so
 cmp al, LF    ; Is it a line feed?
 jz @@line_feed   ;   skip if so
 cmp al, 7    ; Print a bell?
 jz @@beep    ;   do beep
 cmp al, CR    ; Is it a carriage return?
 jz @@carriage_return  ;   skip if so
 mov bl, [bp+4]   ; Else write at cursor position
 mov ah, 0Ah
 mov cx, 1    ;   one time
 int 10h
 inc dl    ; Advance cursor
 cmp dl, [ds:4Ah]   ;   check for line overflow
 jnz @@position
 mov dl, 0    ; Overflowed, then fake
 jmp short @@line_feed  ;   new line

@@back_space:
 cmp dl, 0    ; At start of line?
 jz @@position   ;   skip if so
 dec dl    ; Else back up
 jmp short @@position  ;   join common code

@@beep: mov bl, 1    ; Do a short
 call beep    ;   beep
 ret

@@carriage_return:
 mov dl, 0    ; Position to start of line
; jmp short @@position

@@position:
 mov bl, [ds:62h]   ; Get active video page (0-7)
 shl bl, 1    ;   as word index
 mov bh, 0    ;   clear high order
 mov [bx+50h], dx   ; Remember the cursor position
 jmp set_cursor   ;   set 6845 cursor hardware

@@line_feed:
 cmp dh, 18h    ; Done all 24 lines on page?
 jz @@scroll   ;   yes, scroll
 inc dh    ; Else advance line
 jnz @@position

@@scroll:
 mov ah, 2    ; Position cursor at line start
 int 10h
 call mode_check   ; Is this text mode?
 mov bh, 0
 jb @@scroll_up   ; Skip if text mode
 mov ah, 8
 int 10h    ;   else read attribute
 mov bh, ah

@@scroll_up:
 mov ah, 6    ; Now prepare to
 mov al, 1    ;   scroll
 xor cx, cx    ;   the
 mov dh, 18h    ;   page
 mov dl, [ds:4Ah]   ;   up
 dec dl
 int 10h
 ret

endp int_10_func_14


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Function 15: Return current video state
;---------------------------------------------------------------------------------------------------
proc int_10_func_15 near

 mov al, [ds:4Ah]   ; Get current video state
 mov [bp+3], al   ;   columns
 mov al, [ds:49h]
 mov [bp+2], al   ;   mode
 mov al, [ds:62h]
 mov [bp+5], al   ;   page
 ret

endp int_10_func_15


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Internal: Video mode check
;---------------------------------------------------------------------------------------------------
proc mode_check near

 push ax    ; Set flags to current mode
 mov al, [ds:49h]   ;   get mode
 cmp al, 7    ;   equal if mono
 jz @@done
 cmp al, 4
 cmc
 jnb @@done    ;   carry set on graphics
 sbb al, al
 stc

@@done: pop ax
 ret

endp mode_check


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Internal: Calculate dot offset
;---------------------------------------------------------------------------------------------------
proc dot_offset near

 mov al, 50h    ; Dots in character position
 xor si, si
 shr dl, 1    ; Two bytes/char position
 jnb @@calc    ;   no overflow
 mov si, 2000h   ; Else on other video plane

@@calc: mul dl    ; Multiply position by row
 add si, ax    ;   add in column position
 mov dx, cx    ; Copy column position
 mov cx, 302h   ;   regular char size
 cmp [byte ds:49h], 6  ; Mode 640x200, b/w?
 pushf
 jnz @@done    ;   skip if not
 mov cx, 703h   ; Else special char size

@@done: and ch, dl
 shr dx, cl
 add si, dx
 xchg cl, ch
 popf
 ret

endp dot_offset


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Internal: Read light pen position
;---------------------------------------------------------------------------------------------------
proc pen_pos near

 call @@pen_xy   ; Read light pen position high
 mov ch, al    ;   save in ch
 inc ah
 call @@pen_xy   ; Read light pen position low
 mov cl, al    ;   save in cl
 ret

@@pen_xy:
 push dx    ; Read CRT register offset al
 mov dx, [ds:63h]   ;   get active CRT port
 xchg al, ah
 out dx, al    ; Send initialization byte
 inc dl    ;   increment
 in al, dx    ; Read pen position byte back
 pop dx
 ret

endp pen_pos


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Internal: Convert (row, col) coordinates to column count for text modes
;---------------------------------------------------------------------------------------------------
proc text_rc_col near

 mov bh, 0    ; Convert Row, Col, Page -> Col
 shl bx, 1    ;   two bytes/column
 mov ax, [bx+50h]   ; Get page number in ax
      ;   join common code
rc_to_col:
 push bx    ; Map (ah=row, al=col) to col
 mov bl, al
 mov al, ah
 mul [byte ds:4Ah]   ; Multiply Row x (Row/Column)
 mov bh, 0
 add ax, bx    ;   add in existing col
 shl ax, 1    ;   times 2 because 2 bytes/col
 pop bx
 ret

endp text_rc_col


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Internal: Convert (row, col) coordinates to column count for graphics modes
;---------------------------------------------------------------------------------------------------
proc gfx_rc_col near

 push bx    ; Convert (row, col) -> col
 mov bl, al    ;   save column
 mov al, ah    ;   get row
 mul [byte ds:4Ah]   ; Multiply by columns/row
 shl ax, 1
 shl ax, 1
 mov bh, 0
 add ax, bx    ; Add in columns
 pop bx
 ret

endp gfx_rc_col


;---------------------------------------------------------------------------------------------------
; Interrupt 10h - Internal: Set 6845 cursor position
;---------------------------------------------------------------------------------------------------
proc set_cursor near							; F764

 shr bl, 1    ; Sets 6845 cursor position
 cmp [ds:62h], bl   ;   is this page visible?
 jnz @@done    ; No, do nothing in hardware

move_cursor:
 call text_rc_col   ; Map row, col, page to col
 add ax, [ds:4Eh]   ;   + byte offset, regen register
 shr ax, 1
 mov cx, ax
 mov ah, 0Eh    ; Tell 6845 video controller
      ;   to position the cursor
out_6845:
 mov al, ch    ; Send ch, cl through CRT register ah
 call send_ax    ;   send ch
 inc ah    ;   increment
 mov al, cl    ;   send cl

send_ax:
 push dx
 mov dx, [ds:63h]   ; Load active video port
 xchg al, ah
 out dx, al    ; Send high order
 xchg al, ah
 inc dl
 out dx, al    ;   low order
 pop dx

@@done: ret

endp set_cursor


;--------------------------------------------------------------------------------------------------
; Slows the system clock rate while accessing the floppy drive
;--------------------------------------------------------------------------------------------------
ifdef SLOW_FLOPPY    ; Run floppy at SLOWEST speed
proc floppy_speed near

 in al, 61h    ; Toggle speed on Floppy Disk
 push ax    ;   save old clock rate
 and al, 11110011b   ;   load slowest clock rate
 out 61h, al    ;   slow down to 4.77 mHz
 call floppy_exec   ; Execute the I/O request
 pop ax    ;   restore old clock rate
 out 61h, al    ;   from saved clock byte
 ret

endp floppy_speed
endif


;--------------------------------------------------------------------------------------------------
; Give user option to boot ROM BASIC if present, otherwise display "No ROM BASIC" message
;--------------------------------------------------------------------------------------------------
proc boot_basic near						; F791

 xor cx, cx
 mov es, cx
 mov cx, [es:62h]   ; Get int 18h (BASIC) segment in cx

 xor bl, bl
 add bh, 3
 mov ax, bx
 call locate    ; Locate cursor

 cmp cx, 0F600h   ; If ROM BASIC is present segment will be F600h
 je @@have_basic

 mov si, offset str_no_basic  ; No BASIC, display message
 call print

 mov al, 8    ; Pause for a bit so user can read it
 xor cx, cx
@@delay:
@@delay_2:
 loop  @@delay_2
 dec al
 jnz @@delay

 ret														; E4AD

@@have_basic:												; F7BB
 mov si, offset str_boot_basic ; ROM BASIC present, display message
 call print

 xor bl, bl    ; If bl=1 user pressed space
 xor cx, cx    ; Loop 65536 times
@@loop:
 mov ah, 01h
 int 16h    ; Check for keypress			' F7C7
 jz @@none    ; Abort if none
 xor ah, ah
 int 16h    ; Flush keystroke from buffer
 cmp al, ' '    ; Was it space?
 je @@basic    ; Yes, boot BASIC
@@none:
 loop @@loop
 ret

@@basic:
 int 18h    ; Boot ROM BASIC

endp boot_basic


str_boot_basic db 'Press SPACE to boot ROM BASIC...', 0
str_no_fpu db 'No FPU)', 0
str_8087 db '8087 FPU)', 0
str_error db 'System Error: ', 0, CR, LF, 'Continue? ', 0
str_system db 'System ', 194, ' ', 0
str_last_line db 192, 0


;---------------------------------------------------------------------------------------------------
; Interrupt 12h - Memory Size
;---------------------------------------------------------------------------------------------------
 entry 0F841h    ; IBM entry for memory size
proc int_12 far

 sti     ; Kbytes of memory present
 push ds
 mov ax, 40h
 mov ds, ax
 mov ax, [ds:13h]   ; ax = memory size, kilobytes
 pop ds
 iret

endp int_12


;---------------------------------------------------------------------------------------------------
; Interrupt 11h - Equipment Check
;---------------------------------------------------------------------------------------------------
 entry 0F84Dh    ; IBM entry for equipment check
proc int_11 far

 sti     ; Equipment present
 push ds
 mov ax, 40h
 mov ds, ax
 mov ax, [ds:10h]   ; ax = equipment byte contents
 pop ds
 iret

endp int_11


;---------------------------------------------------------------------------------------------------
; Interrupt 15h - Cassette
;---------------------------------------------------------------------------------------------------
 entry 0F859h    ; IBM entry for cassette interrupt
proc int_15 far

 stc     ; Cassette service (error ret)
 mov ah, 86h
 retf 2

endp int_15


;---------------------------------------------------------------------------------------------------
; Interrupt 2h - Non-Maskable Interrupt
;---------------------------------------------------------------------------------------------------
 entry 0F85Fh    ; IBM non-maskable interrupt entry
proc int_2 far

 push ax    ; Non-maskable interrupt
 in al, 62h
 test al, 11000000b   ; Get cause of interrupt
 jnz @@parity   ;   parity error
 jmp @@end    ;   math coprocessor (?)

@@parity:
 push bx    ; Parity error bomb
 push cx
 push dx
 push si
 push di
 push bp
 push ds
 push es
 mov ax, 40h    ; Load data segment
 mov ds, ax
 call video_init   ;   clear/init screen
 push ds
 push cs    ; Point ds at ROM
 pop ds
 mov si, offset str_parity_error ; si --> Parity message
 call print    ;   print
 pop ds    ;   restore ds
 mov ax, 11h    ; Back cursor over ? marks
 call locate    ;   with call
 mov al, 0
 out 0A0h, al   ;   disable NMI interrupts
 mov dx, 61h
 in al, dx    ; Get machine flags
 or al, 00110000b   ;   disable parity int
 out dx, al    ; Put out new flags
 and al, 11001111b   ;   enable parity int
 out dx, al    ; Put out new flags
 mov cl, 6
 mov bx, [ds:13h]   ; Get memory size (K bytes)
 shl bx, cl
 inc dx    ;   now paragraphs
 xor ax, ax
 mov ds, ax

@@next_para:
 mov cx, 10h    ; Iterations to check
 xor si, si

@@next_byte:
 mov ah, [si]   ; Read the byte (dummy)
 in al, dx    ;   and read status
 test al, 11000000b   ;   to see what happened
 jnz @@display_addr   ; Read caused parity error
 inc si    ;   else advance pointer
 loop @@next_byte   ;   and try next byte

 mov ax, ds
 inc ax    ;   next paragraph
 mov ds, ax
 cmp ax, bx
 jnz @@next_para   ; More paragraphs to check
 jmp short @@prompt   ;   else flakey error

@@display_addr:
 mov [si], ah   ; Save offset in paragraph
 mov ax, ds
 call double_number   ; Print segment
 mov ax, si
 call digit    ; Print offset

@@prompt:
 mov ax, 16h    ; Where to position cursor
 call locate    ;   position cursor
 push ds
 push cs
 pop ds
 mov si, offset str_parity_error_2 ; Continue ?
 call print    ;   ask the user
 pop ds
 in al, 21h    ; Get interrupt masks
 push ax    ;   save them
 mov al, 11111100b
 out 21h, al    ; Disable all but keyboard
 sti     ;   enable interrupt system
 call get_key    ; Get keyboard character
 push ax    ;   save it
 call out_char   ; Print ascii character
 pop ax    ;   restore
 cmp al, 'Y'    ; User wants to continue?
 jz @@continue   ;   continue
 cmp al, 'y'    ; Look for little case "y"
 jz @@continue   ;   continue
 jmp cold_boot   ; Retry on cold reboot

@@continue:
 call clear_screen   ; Clear display
 pop ax
 out 21h, al    ; Restore interrupt system state
 mov dx, 61h    ; Dismiss the NMI interrupt
 in al, dx    ;   read in machine flags
 or al, 00110000b
 out dx, al    ; Write out, parity disabled
 and al, 11001111b   ;   clears parity error
 out dx, al    ; Write out, parity enabled
 mov al, 80h
 out 0A0h, al   ; Enable NMI interrupts
 pop es
 pop ds
 pop bp
 pop di
 pop si
 pop dx
 pop cx
 pop bx

@@end: pop ax
 iret

endp int_2


str_parity_error db 'Parity error at: ?????', 0
str_parity_error_2 db CR, LF, 'Continue? ', 0


;--------------------------------------------------------------------------------------------------
; Display al as hex digit in ASCII
;--------------------------------------------------------------------------------------------------
proc digit near

 push ax    ; Print hex digit in al
 and al, 0Fh
 cmp al, 9
 jbe @@low
 add al, 'A'-'9'-1
@@low:
 add al, '0'    ; Make ascii digit
 call out_char   ;   print it
 pop ax
 ret

endp digit


;--------------------------------------------------------------------------------------------------
; Display value in al as 2-character ASCII hex number
;--------------------------------------------------------------------------------------------------
proc number near

 push ax    ; Save number
 mov cl, 4
 shr al, cl
 call digit    ; Out first digit
 pop ax
 call digit    ; Out second digit
 ret

endp number


;--------------------------------------------------------------------------------------------------
; Display value in ax as 3-character ASCII hex number
;--------------------------------------------------------------------------------------------------
proc big_number near

 push ax    ; Unsigned word
 mov al, ah
 call digit
big_number_entry:
 pop ax
 call number
 ret

endp big_number


;--------------------------------------------------------------------------------------------------
; Display character
;--------------------------------------------------------------------------------------------------
proc out_char near

 push bx
 push ax
 mov ah, 0Eh    ; Teletype print service
 mov bl, 7    ;   normal intensity
 int 10h
 pop ax
 pop bx
 ret

endp out_char


;--------------------------------------------------------------------------------------------------
; Display null-terminated string (si)
;--------------------------------------------------------------------------------------------------
proc print near

@@loop:
 lodsb     ; Print zero terminated string
 or al, al    ; Terminator in ax?
 jz @@done
 call out_char   ; Print character in ax
 jmp @@loop    ;   back for more
@@done:
 ret

endp print


;--------------------------------------------------------------------------------------------------
; Positions display cursor
;--------------------------------------------------------------------------------------------------
proc locate near

 push dx
 push bx
 mov dx, ax    ; Get position for cursor
 mov ah, 2
 mov bh, 0    ;   page 0
 int 10h
 pop bx
 pop dx
 ret

endp locate


;--------------------------------------------------------------------------------------------------
; Display character with color attribute (or inverse for monochrome), cx=times to repeat character
;--------------------------------------------------------------------------------------------------
proc color_out_char near

 push dx
 push bx

 push ax
 push cx

 mov ah, 03h    ; Get cursor position
 xor bh, bh
 int 10h

 pop cx
 pop ax
 push ax

 mov bx, 070h   ; Mono uses inverse attribute
 mov ah, [es:49h]   ; Get CRT mode
 cmp ah, 7
 jz @@is_mono   ;   monochrome
 mov bx, 1Fh    ; Color uses white on blue
@@is_mono:
 mov ah, 09h    ; Write character and attribute
 int 10h

 mov ah, 02h    ; Set cursor position
 xor bh, bh
 inc dl    ; Advance cursor 1 space
 int 10h

 pop ax
 pop bx
 pop dx
 ret

endp color_out_char


;--------------------------------------------------------------------------------------------------
; Display null-terminated string (si) in color for BIOS title bar
;--------------------------------------------------------------------------------------------------
proc color_print near

@@loop:
 mov cx, 1
 lodsb     ; Print zero terminated string
 or al, al
 jz @@done    ; Terminator in ax?
 call color_out_char   ; Print character in ax
 jmp @@loop    ;   back for more

@@done:
 mov cx, 54    ; Extend color bar
 call color_out_char
 ret

endp color_print


;--------------------------------------------------------------------------------------------------
; Initialize video card
;--------------------------------------------------------------------------------------------------
proc video_init near

 mov ah, [ds:10h]   ; Get equipment byte
 and ah, 00110000b   ;   extract CRT
 mov al, 0    ;   null low
 cmp ah, 00110000b   ; Monochrome?
 jz @@init    ;   yes
 mov al, 1    ; CGA 40 x 25?
 cmp ah, 00010000b   ;   yes
 jz @@init    ; CGA 80 x 25?
 mov al, 3    ;   yes
@@init:
 mov ah, 0    ; Setup subfunction
 int 10h    ;   to video
 ret

endp video_init


;--------------------------------------------------------------------------------------------------
; Waits for a keypress and then returns it (ah=scan code, al=ASCII)
;--------------------------------------------------------------------------------------------------
proc get_key near

 mov ah, 0    ; Read keyboard key
 int 16h
 ret

endp get_key


;--------------------------------------------------------------------------------------------------
; PC speaker beep (length in bl)
;--------------------------------------------------------------------------------------------------
proc beep near

 push ax
 push cx
 mov al, 10110110b   ; Timer IC 8253 square waves
 out 43h, al    ;   channel 2, speaker
 mov ax, 528h   ; Get countdown constant word
 out 42h, al    ;   send low order
 mov al, ah    ;   load high order
 out 42h, al    ;   send high order
 in al, 61h    ; Read IC 8255 machine status
 push ax
 or al, 00000011b
 out 61h, al    ; Turn speaker on
 xor cx, cx
@@delay:
 loop @@delay
 dec bl
 jnz @@delay
 pop ax
 out 61h, al    ; Turn speaker off
 pop cx
 pop ax
 ret

endp beep


;--------------------------------------------------------------------------------------------------
; 8-bit checksum
;--------------------------------------------------------------------------------------------------
proc checksum near

 mov cx, 2000h   ; Bytes in 2764 eprom

checksum_entry:
 mov al, 0    ;   zero checksum
@@loop:
 add al, [bx]   ; Add byte to checksum
 inc bx    ;   bx --> next byte
 loop @@loop    ;   loop until done
 or al, al    ; Set condition codes
 ret     ;   and return

endp checksum


;--------------------------------------------------------------------------------------------------
; Tests 1K of memory at es:0000 and increments es
;--------------------------------------------------------------------------------------------------
proc mem_test near

 mov bx, 0400h   ; Load bytes to test
 mov al, 55h

@@pattern_1:
 xor di, di    ; Pattern #1, 55h bytes
 mov cx, bx
 repz stosb    ; Fill memory, pattern #1
 xor di, di
 mov cx, bx
 repz scasb    ; Scan memory for not pattern #1
 jcxz @@pattern_2
 stc     ; Flunked
 ret

@@pattern_2:
ifndef FAST_MEM_CHECK
 xor di, di    ; Pattern #2 - 0AAh bytes
 mov cx, bx
 not al
 repz stosb    ; Fill memory, pattern #2
 xor di, di
 mov cx, bx
 repz scasb    ; Scan memory for not pattern #2
 jcxz @@pattern_3
 stc     ; Flunked
 ret

@@pattern_3:
 xor di, di    ; Pattern #3 - 01h bytes
 mov cx, bx
 mov al, 1
 repz stosb    ; Fill memory, pattern #3
 xor di, di
 mov cx, bx
 repz scasb    ; Scan memory for not pattern #3
 jcxz @@pattern_4
 stc     ; Flunked
 ret

@@pattern_4:
 xor di, di    ; Pattern #4 - 0h bytes
 mov cx, bx
 dec al
 repz stosb    ; Fill memory,pattern #4
 xor di, di
 mov cx, bx
 repz scasb    ; Scan memory for not pattern #4
 jcxz @@done
 stc     ; Flunked
 ret
endif

@@done:
 mov ax, es
 add ax, 40h    ; Add 40h to segment number
 mov es, ax
 ret     ; Passed

endp mem_test


;---------------------------------------------------------------------------------------------------
; 8x8 Graphics Character Set (chars 0-127)
;---------------------------------------------------------------------------------------------------
 entry 0FA6Eh    ; IBM graphics character set entry

gfx_chars db 000h, 000h, 000h, 000h, 000h, 000h, 000h, 000h  ;   0  nul
  db 07Eh, 081h, 0A5h, 081h, 0BDh, 099h, 081h, 07Eh  ;   1  soh
  db 07Eh, 0FFh, 0DBh, 0FFh, 0C3h, 0E7h, 0FFh, 07Eh  ;   2  stx
  db 06Ch, 0FEh, 0FEh, 0FEh, 07Ch, 038h, 010h, 000h  ;   3  etx
  db 010h, 038h, 07Ch, 0FEh, 07Ch, 038h, 010h, 000h  ;   4  eot
  db 038h, 07Ch, 038h, 0FEh, 0FEh, 07Ch, 038h, 07Ch  ;   5  enq
  db 010h, 010h, 038h, 07Ch, 0FEh, 07Ch, 038h, 07Ch  ;   6  ack
  db 000h, 000h, 018h, 03Ch, 03Ch, 018h, 000h, 000h  ;   7  bel
  db 0FFh, 0FFh, 0E7h, 0C3h, 0C3h, 0E7h, 0FFh, 0FFh  ;   8  bs
  db 000h, 03Ch, 066h, 042h, 042h, 066h, 03Ch, 000h  ;   9  ht
  db 0FFh, 0C3h, 099h, 0BDh, 0BDh, 099h, 0C3h, 0FFh  ;  10  lf
  db 00Fh, 007h, 00Fh, 07Dh, 0CCh, 0CCh, 0CCh, 078h  ;  11  vt
  db 03Ch, 066h, 066h, 066h, 03Ch, 018h, 07Eh, 018h  ;  12  ff
  db 03Fh, 033h, 03Fh, 030h, 030h, 070h, 0F0h, 0E0h  ;  13  cr
  db 07Fh, 063h, 07Fh, 063h, 063h, 067h, 0E6h, 0C0h  ;  14  so
  db 099h, 05Ah, 03Ch, 0E7h, 0E7h, 03Ch, 05Ah, 099h  ;  15  si
  db 080h, 0E0h, 0F8h, 0FEh, 0F8h, 0E0h, 080h, 000h  ;  16  dle
  db 002h, 00Eh, 03Eh, 0FEh, 03Eh, 00Eh, 002h, 000h  ;  17  dc1
  db 018h, 03Ch, 07Eh, 018h, 018h, 07Eh, 03Ch, 018h  ;  18  dc2
  db 066h, 066h, 066h, 066h, 066h, 000h, 066h, 000h  ;  19  dc3
  db 07Fh, 0DBh, 0DBh, 07Bh, 01Bh, 01Bh, 01Bh, 000h  ;  20  dc4
  db 03Eh, 063h, 038h, 06Ch, 06Ch, 038h, 0CCh, 078h  ;  21  nak
  db 000h, 000h, 000h, 000h, 07Eh, 07Eh, 07Eh, 000h  ;  22  syn
  db 018h, 03Ch, 07Eh, 018h, 07Eh, 03Ch, 018h, 0FFh  ;  23  etb
  db 018h, 03Ch, 07Eh, 018h, 018h, 018h, 018h, 000h  ;  24  can
  db 018h, 018h, 018h, 018h, 07Eh, 03Ch, 018h, 000h  ;  25  em
  db 000h, 018h, 00Ch, 0FEh, 00Ch, 018h, 000h, 000h  ;  26  sub
  db 000h, 030h, 060h, 0FEh, 060h, 030h, 000h, 000h  ;  27  esc
  db 000h, 000h, 0C0h, 0C0h, 0C0h, 0FEh, 000h, 000h  ;  28  fs
  db 000h, 024h, 066h, 0FFh, 066h, 024h, 000h, 000h  ;  29  gs
  db 000h, 018h, 03Ch, 07Eh, 0FFh, 0FFh, 000h, 000h  ;  30  rs
  db 000h, 0FFh, 0FFh, 07Eh, 03Ch, 018h, 000h, 000h  ;  31  us
  db 000h, 000h, 000h, 000h, 000h, 000h, 000h, 000h  ;  32  space
  db 030h, 078h, 078h, 030h, 030h, 000h, 030h, 000h  ;  33  !
  db 06Ch, 06Ch, 06Ch, 000h, 000h, 000h, 000h, 000h  ;  34  "
  db 06Ch, 06Ch, 0FEh, 06Ch, 0FEh, 06Ch, 06Ch, 000h  ;  35  #
  db 030h, 07Ch, 0C0h, 078h, 00Ch, 0F8h, 030h, 000h  ;  36  $
  db 000h, 0C6h, 0CCh, 018h, 030h, 066h, 0C6h, 000h  ;  37  %
  db 038h, 06Ch, 038h, 076h, 0DCh, 0CCh, 076h, 000h  ;  38  &
  db 060h, 060h, 0C0h, 000h, 000h, 000h, 000h, 000h  ;  39  '
  db 018h, 030h, 060h, 060h, 060h, 030h, 018h, 000h  ;  40  (
  db 060h, 030h, 018h, 018h, 018h, 030h, 060h, 000h  ;  41  )
  db 000h, 066h, 03Ch, 0FFh, 03Ch, 066h, 000h, 000h  ;  42  *
  db 000h, 030h, 030h, 0FCh, 030h, 030h, 000h, 000h  ;  43  +
  db 000h, 000h, 000h, 000h, 000h, 030h, 030h, 060h  ;  44  ,
  db 000h, 000h, 000h, 0FCh, 000h, 000h, 000h, 000h  ;  45  -
  db 000h, 000h, 000h, 000h, 000h, 030h, 030h, 000h  ;  46  .
  db 006h, 00Ch, 018h, 030h, 060h, 0C0h, 080h, 000h  ;  47  /
  db 07Ch, 0C6h, 0CEh, 0DEh, 0F6h, 0E6h, 07Ch, 000h  ;  48  0
  db 030h, 070h, 030h, 030h, 030h, 030h, 0FCh, 000h  ;  49  1
  db 078h, 0CCh, 00Ch, 038h, 060h, 0CCh, 0FCh, 000h  ;  50  2
  db 078h, 0CCh, 00Ch, 038h, 00Ch, 0CCh, 078h, 000h  ;  51  3
  db 01Ch, 03Ch, 06Ch, 0CCh, 0FEh, 00Ch, 01Eh, 000h  ;  52  4
  db 0FCh, 0C0h, 0F8h, 00Ch, 00Ch, 0CCh, 078h, 000h  ;  53  5
  db 038h, 060h, 0C0h, 0F8h, 0CCh, 0CCh, 078h, 000h  ;  54  6
  db 0FCh, 0CCh, 00Ch, 018h, 030h, 030h, 030h, 000h  ;  55  7
  db 078h, 0CCh, 0CCh, 078h, 0CCh, 0CCh, 078h, 000h  ;  56  8
  db 078h, 0CCh, 0CCh, 07Ch, 00Ch, 018h, 070h, 000h  ;  57  9
  db 000h, 030h, 030h, 000h, 000h, 030h, 030h, 000h  ;  58  :
  db 000h, 030h, 030h, 000h, 000h, 030h, 030h, 060h  ;  59  ;
  db 018h, 030h, 060h, 0C0h, 060h, 030h, 018h, 000h  ;  60  <
  db 000h, 000h, 0FCh, 000h, 000h, 0FCh, 000h, 000h  ;  61  =
  db 060h, 030h, 018h, 00Ch, 018h, 030h, 060h, 000h  ;  62  >
  db 078h, 0CCh, 00Ch, 018h, 030h, 000h, 030h, 000h  ;  63  ?
  db 07Ch, 0C6h, 0DEh, 0DEh, 0DEh, 0C0h, 078h, 000h  ;  64  @
  db 030h, 078h, 0CCh, 0CCh, 0FCh, 0CCh, 0CCh, 000h  ;  65  A
  db 0FCh, 066h, 066h, 07Ch, 066h, 066h, 0FCh, 000h  ;  66  B
  db 03Ch, 066h, 0C0h, 0C0h, 0C0h, 066h, 03Ch, 000h  ;  67  C
  db 0F8h, 06Ch, 066h, 066h, 066h, 06Ch, 0F8h, 000h  ;  68  D
  db 0FEh, 062h, 068h, 078h, 068h, 062h, 0FEh, 000h  ;  69  E
  db 0FEh, 062h, 068h, 078h, 068h, 060h, 0F0h, 000h  ;  70  F
  db 03Ch, 066h, 0C0h, 0C0h, 0CEh, 066h, 03Eh, 000h  ;  71  G
  db 0CCh, 0CCh, 0CCh, 0FCh, 0CCh, 0CCh, 0CCh, 000h  ;  72  H
  db 078h, 030h, 030h, 030h, 030h, 030h, 078h, 000h  ;  73  I
  db 01Eh, 00Ch, 00Ch, 00Ch, 0CCh, 0CCh, 078h, 000h  ;  74  J
  db 0E6h, 066h, 06Ch, 078h, 06Ch, 066h, 0E6h, 000h  ;  75  K
  db 0F0h, 060h, 060h, 060h, 062h, 066h, 0FEh, 000h  ;  76  L
  db 0C6h, 0EEh, 0FEh, 0FEh, 0D6h, 0C6h, 0C6h, 000h  ;  77  M
  db 0C6h, 0E6h, 0F6h, 0DEh, 0CEh, 0C6h, 0C6h, 000h  ;  78  N
  db 038h, 06Ch, 0C6h, 0C6h, 0C6h, 06Ch, 038h, 000h  ;  79  O
  db 0FCh, 066h, 066h, 07Ch, 060h, 060h, 0F0h, 000h  ;  80  P
  db 078h, 0CCh, 0CCh, 0CCh, 0DCh, 078h, 01Ch, 000h  ;  81  Q
  db 0FCh, 066h, 066h, 07Ch, 06Ch, 066h, 0E6h, 000h  ;  82  R
  db 078h, 0CCh, 0E0h, 070h, 01Ch, 0CCh, 078h, 000h  ;  83  S
  db 0FCh, 0B4h, 030h, 030h, 030h, 030h, 078h, 000h  ;  84  T
  db 0CCh, 0CCh, 0CCh, 0CCh, 0CCh, 0CCh, 0FCh, 000h  ;  85  U
  db 0CCh, 0CCh, 0CCh, 0CCh, 0CCH, 078h, 030h, 000h  ;  86  V
  db 0C6h, 0C6h, 0C6h, 0D6h, 0FEh, 0EEh, 0C6h, 000h  ;  87  W
  db 0C6h, 0C6h, 06Ch, 038h, 038h, 06Ch, 0C6h, 000h  ;  88  X
  db 0CCh, 0CCh, 0CCh, 078h, 030h, 030h, 078h, 000h  ;  89  Y
  db 0FEh, 0C6h, 08Ch, 018h, 032h, 066h, 0FEh, 000h  ;  90  Z
  db 078h, 060h, 060h, 060h, 060h, 060h, 078h, 000h  ;  91  [
  db 0C0h, 060h, 030h, 018h, 00Ch, 006h, 002h, 000h  ;  92  backslash
  db 078h, 018h, 018h, 018h, 018h, 018h, 078h, 000h  ;  93  ]
  db 010h, 038h, 06Ch, 0C6h, 000h, 000h, 000h, 000h  ;  94  ^
  db 000h, 000h, 000h, 000h, 000h, 000h, 000h, 0FFh  ;  95  _
  db 030h, 030h, 018h, 000h, 000h, 000h, 000h, 000h  ;  96  `
  db 000h, 000h, 078h, 00Ch, 07Ch, 0CCh, 076h, 000h  ;  97  a
  db 0E0h, 060h, 060h, 07Ch, 066h, 066h, 0DCh, 000h  ;  98  b
  db 000h, 000h, 078h, 0CCh, 0C0h, 0CCh, 078h, 000h  ;  99  c
  db 01Ch, 00Ch, 00Ch, 07Ch, 0CCh, 0CCh, 076h, 000h  ; 100  d
  db 000h, 000h, 078h, 0CCh, 0FCh, 0C0h, 078h, 000h  ; 101  e
  db 038h, 06Ch, 060h, 0F0h, 060h, 060h, 0F0h, 000h  ; 102  f
  db 000h, 000h, 076h, 0CCh, 0CCh, 07Ch, 00Ch, 0F8h  ; 103  g
  db 0E0h, 060h, 06Ch, 076h, 066h, 066h, 0E6h, 000h  ; 104  h
  db 030h, 000h, 070h, 030h, 030h, 030h, 078h, 000h  ; 105  i
  db 00Ch, 000h, 00Ch, 00Ch, 00Ch, 0CCh, 0CCh, 078h  ; 106  j
  db 0E0h, 060h, 066h, 06Ch, 078h, 06Ch, 0E6h, 000h  ; 107  k
  db 070h, 030h, 030h, 030h, 030h, 030h, 078h, 000h  ; 108  l
  db 000h, 000h, 0CCh, 0FEh, 0FEh, 0D6h, 0C6h, 000h  ; 109  m
  db 000h, 000h, 0F8h, 0CCh, 0CCh, 0CCh, 0CCh, 000h  ; 110  n
  db 000h, 000h, 078h, 0CCh, 0CCh, 0CCh, 078h, 000h  ; 111  o
  db 000h, 000h, 0DCh, 066h, 066h, 07Ch, 060h, 0F0h  ; 112  p
  db 000h, 000h, 076h, 0CCh, 0CCh, 07Ch, 00Ch, 01Eh  ; 113  q
  db 000h, 000h, 0DCh, 076h, 066h, 060h, 0F0h, 000h  ; 114  r
  db 000h, 000h, 07Ch, 0C0h, 078h, 00Ch, 0F8h, 000h  ; 115  s
  db 010h, 030h, 07Ch, 030h, 030h, 034h, 018h, 000h  ; 116  t
  db 000h, 000h, 0CCh, 0CCh, 0CCh, 0CCh, 076h, 000h  ; 117  u
  db 000h, 000h, 0CCh, 0CCh, 0CCh, 078h, 030h, 000h  ; 118  v
  db 000h, 000h, 0C6h, 0D6h, 0FEh, 0FEh, 06Ch, 000h  ; 119  w
  db 000h, 000h, 0C6h, 06Ch, 038h, 06Ch, 0C6h, 000h  ; 120  x
  db 000h, 000h, 0CCh, 0CCh, 0CCh, 07Ch, 00Ch, 0F8h  ; 121  y
  db 000h, 000h, 0FCh, 098h, 030h, 064h, 0FCh, 000h  ; 122  z
  db 01Ch, 030h, 030h, 0E0h, 030h, 030h, 01Ch, 000h  ; 123  {
  db 018h, 018h, 018h, 000h, 018h, 018h, 018h, 000h  ; 124  |
  db 0E0h, 030h, 030h, 01Ch, 030h, 030h, 0E0h, 000h  ; 125  }
  db 076h, 0DCh, 000h, 000h, 000h, 000h, 000h, 000h  ; 126  ~
  db 000h, 010h, 038h, 06Ch, 0C6h, 0C6h, 0FEh, 000h  ; 127  del


;---------------------------------------------------------------------------------------------------
; Interrupt 1Ah - Time Of Day Clock
;---------------------------------------------------------------------------------------------------
 entry 0FE6Eh    ; IBM entry, time_of_day clock
proc int_1A far

 sti     ; User time_of_day BIOS service
 push ds
 push ax
 mov ax, 40h
 mov ds, ax
 pop ax    ; Get request type
 cli     ;   pause clock
 or ah, ah
 jz @@read    ; Read time, ah=0
 dec ah
 jnz @@end    ;   invalid request

@@set:
 mov [ds:6Ch], dx   ; Set time, ah=1
 mov [ds:6Eh], cx   ;   set time high
 mov [byte ds:70h], 0  ;   not a new day
 jmp short @@end

@@read:
 mov cx, [ds:6Eh]   ; Read low order time
 mov dx, [ds:6Ch]   ;   high order time
 call @@reset    ; Read resets overflow

@@end: sti     ; Resume clock
 pop ds
 iret

@@reset:
 mov al, [ds:70h]   ; Zero the overflow and return
 xor [ds:70h], al   ;   previous status in flags
 retn

endp int_1A


;---------------------------------------------------------------------------------------------------
; Interrupt 8h - Hardware Clock
;---------------------------------------------------------------------------------------------------
 entry 0FEA5h    ; IBM entry, hardware clock
proc int_8 far

 sti     ; Routine services clock tick
 push ds
 push dx
 push ax
 mov ax, 40h
 mov ds, ax
 dec [byte ds:40h]   ; Decrement motor count
 jnz @@increment   ;   not time to shut off
 and [byte ds:3Fh], 11110000b ; Else show motor off
 mov al, 0Ch    ;   send motor off
 mov dx, 3F2h   ;   to the floppy
 out dx, al    ;   disk controller

@@increment:
 inc [word ds:6Ch]   ; Bump low order time of day
 jnz @@check_midnight  ;   no carry
 inc [word ds:6Eh]   ; Bump high order time of day

@@check_midnight:
 cmp [word ds:6Eh], 18h  ; Is it midnight yet?
 jnz @@user    ;   no
 cmp [word ds:6Ch], 0B0h  ; Possibly, check low order
 jnz @@user    ;   not midnight
 mov [word ds:6Eh], 0  ; Midnight, reset high order
 mov [word ds:6Ch], 0  ;   low order ticks
 mov [byte ds:70h], 1  ; Show new day since last read

@@user:
 int 1Ch    ; Execute user clock service
 mov al, 20h    ;   send end_of_interrupt
 out 20h, al    ;   to 8259 interrupt chip
 pop ax
 pop dx
 pop ds
 iret

endp int_8


;---------------------------------------------------------------------------------------------------
; Interrupt Vectors
;---------------------------------------------------------------------------------------------------
 entry 0FEF3h    ; IBM entry, interrupt vector table

vectors dw int_8    ; Timer tick
 dw int_9    ; Keyboard attention
 dw ignore_int   ; Reserved
 dw ignore_int   ; Reserved for COM2 serial I/O
 dw ignore_int   ; Reserved for COM1 serial I/O
 dw ignore_int   ; Reserved for hard disk attention
 dw int_E    ; Floppy disk attention
 dw ignore_int   ; Reserved for parallel printer
 dw int_10    ; Video BIOS services
 dw int_11    ; Equipment present
 dw int_12    ; Memory present
 dw int_13    ; Disk BIOS services
 dw int_14    ; Serial communication services
 dw int_15    ; Cassette BIOS services
 dw int_16    ; Keyboard BIOS services
 dw int_17    ; Parallel printer services
 dw ignore_int   ; ROM Basic (setup later)
 dw int_19    ; Bootstrap
 dw int_1A    ; Timer BIOS services
 dw dummy_int   ; Keyboard break user service
 dw dummy_int   ; System tick user service
 dw int_1D    ; Video parameter table
 dw int_1E    ; Disk parameter table
 dw ?    ; Graphic character table pointer


;---------------------------------------------------------------------------------------------------
; Unexpected Interrupt
;---------------------------------------------------------------------------------------------------
 entry 0FF23h    ; IBM entry, nonsense interrupt
proc ignore_int far

 push ds    ; Unexpected interrupts go here
 push dx
 push ax
 mov ax, 40h
 mov ds, ax
 mov al, 0Bh    ; What IRQ caused this?
 out 20h, al
 nop
 in al, 20h    ;   (read IRQ level)
 mov ah, al
 or al, al
 jnz @@hardware
 mov al, 0FFh   ; Not hardware, say 0FFh IRQ
 jmp short @@done

@@hardware:
 in al, 21h    ; Clear the IRQ
 or al, ah
 out 21h, al
 mov al, 20h    ; Send end_of_interrupt code
 out 20h, al    ;   to 8259 interrupt chip

@@done:
 mov [ds:6Bh], ah   ; Save last nonsense interrupt
 pop ax
 pop dx
 pop ds
 iret

endp ignore_int


str_h  db 'h', 0


;---------------------------------------------------------------------------------------------------
; Dummy Interrupt
;---------------------------------------------------------------------------------------------------
 entry 0FF53h    ; IBM entry, dummy interrupts
proc dummy_int far
;int_1B:     ; Keyboard break user service
;int_1C:     ; Clock    tick  user service
 iret

endp dummy_int


;---------------------------------------------------------------------------------------------------
; Interrupt 5h - Print Screen
;---------------------------------------------------------------------------------------------------
 entry 0FF54h    ; IBM entry, print screen
proc int_5 far    ; Print screen service

 sti
 push ds
 push ax
 push bx
 push cx
 push dx
 mov ax, 40h
 mov ds, ax
 cmp [byte ds:100h], 1  ; Print screen in progress?
 jz @@done    ;   yes, ignore
 mov [byte ds:100h], 1  ; Flag print screen in progress
 call print_cr_lf   ;   begin new line
 mov ah, 0Fh
 int 10h    ; Get current video state
 push ax    ;   save it
 mov ah, 3
 int 10h    ; Read cursor position
 pop ax    ;   retrieve video state
 push dx    ;   save cursor position
 mov ch, 19h    ; Do 25 rows
 mov cl, ah    ;   columns in current mode
 xor dx, dx    ; Start printing from (0, 0)

@@loop:
 mov ah, 2    ; Set cursor to position
 int 10h
 mov ah, 8    ;   and read character
 int 10h
 or al, al    ; Nulls are special case
 jnz @@print_char
 mov al, ' '    ;   convert to spaces

@@print_char:
 push dx
 xor dx, dx
 mov ah, dl    ; Function = Print character
 int 17h
 pop dx
 test ah, 00100101b   ; Successful print
 jz @@next_char
 mov [byte ds:100h], 0FFh  ; No, error in Print Screen
 jmp short @@restore_cursor

@@next_char:
 inc dl    ; Increment column count
 cmp cl, dl
 jnz @@loop    ;   in range, continue
 mov dl, 0
 call print_cr_lf   ; Else print new line
 inc dh    ;   add another row
 cmp dh, ch    ; Done all 25 rows?
 jnz @@loop    ;   no, continue
 mov [byte ds:100h], 0  ; Show done Print Screen OK

@@restore_cursor:
 pop dx    ; Get saved cursor position
 mov ah, 2
 int 10h    ;   restore it

@@done:
 pop dx
 pop cx
 pop bx
 pop ax
 pop ds
 iret

endp int_5


str_timer db 195, ' Timer', 0


;--------------------------------------------------------------------------------------------------
; Prints CR+LF on the printer
;--------------------------------------------------------------------------------------------------
 entry 0FFCBh    ; IBM entry, print CR+LF
proc print_cr_lf near

 push dx    ; Print CR+LF, on line printer
 xor dx, dx
 mov ah, dl    ; Function = print
 mov al, LF    ; LF
 int 17h
 mov ah, 0
 mov al, CR    ; CR
 int 17h
 pop dx
 ret

endp print_cr_lf


;--------------------------------------------------------------------------------------------------
; Sets proper underline cursor shape
;--------------------------------------------------------------------------------------------------
proc set_cursor_size near

 mov [word ds:60h], 0607h  ; Assume color cursor (line 6-7)
 cmp [byte ds:49h], 7  ; Mono video?
 jne @@done    ; Nope, done
 mov [word ds:60h], 0B0Ch  ; Otherwise use mono cursor (line B-C)
@@done:
 ret

endp set_cursor_size


;--------------------------------------------------------------------------------------------------
; Power-On Entry Point
;--------------------------------------------------------------------------------------------------
 entry 0FFF0h    ; Hardware power reset entry
proc power far    ;   CPU begins here on power up

 jmpf 0F000h, cold_boot

endp power


;--------------------------------------------------------------------------------------------------
; BIOS Release Date and Signature
;--------------------------------------------------------------------------------------------------
 entry 0FFF5h
 db "04/23/08"   ; Release date (MM/DD/YY)
      ;   originally 08/23/87
 entry 0FFFEh
 db 0FEh    ; Computer type (XT)
; db 0    ; Checksum byte (8K ROM must sum to 0 mod 256)

ends code

end
