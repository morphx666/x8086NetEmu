;
;  rombios.asm
;  Joris van Rantwijk
;

; Common calling convention for internal subroutines (not always used):
;  preserved by subroutine: bx, si, di, bp, segregs
;  destroyed by subroutine: ax, cx, dx
;  ds=0x0040

; %define DEBUG_KEYS
%define DEBUG_VIDEO_READCHAR

%define startaddr 0x8000
%define JUMPTO(ofs)  TIMES (romstart+(ofs)-startaddr-$) db 0

; CPU constants
%define FlagCF			0x0001
%define FlagZF			0x0040

; BIOS data area
%define BiosEquip		0x10
%define BiosMemSize		0x13
%define BiosKeyShift1		0x17
%define BiosKeyShift2		0x18
%define BiosKeyAlt		0x19
%define BiosKeyBufRPtr		0x1a
%define BiosKeyBufWPtr		0x1c
%define BiosVMode		0x49
%define BiosVColumns		0x4a
%define BiosVPageSize		0x4c
%define BiosVPageStart		0x4e
%define BiosVCursorPos		0x50
%define BiosVCursorType		0x60
%define BiosVPageNr		0x62
%define BiosVCrtBase		0x63
%define BiosVModeReg		0x65
%define BiosVColorReg		0x66
%define BiosTimerCount		0x6c
%define BiosTimerDayFlag	0x70
%define BiosBreakFlag		0x71
%define BiosPostFlag		0x72
%define BiosKeyBufStart		0x80
%define BiosKeyBufEnd		0x82
%define BiosScreenRows		0x84

; Hardware constants
%define CgaCrtReg		0x03d4
%define CgaModeReg		0x03d8
%define CgaColorReg		0x03d9
%define CgaStatusReg		0x03da

cpu 186
org startaddr
romstart:

; ========================================
;  F000:E000 - Start of ROM BIOS
; ========================================
JUMPTO(0xe000)
biosstart:
  db 'Retro BIOS',0


; ========================================
;  F000:E05B - POST Entry Point
; ========================================
JUMPTO(0xe05b)
bootentry:

; Setup segments and stack
  cli
  xor ax,ax
  mov es,ax			; ES = 0
  mov ss,ax
  mov sp,0x7c00			; Temporary stack at 0000:7C00
  mov ax,0x0040
  mov ds,ax			; DS = 0040

; Setup I8237 DMA controller
  mov al,0
  out 0x81,al			; Reset DMA page registers
  out 0x82,al
  out 0x83,al
  out 0x0d,al			; Reset controller
  mov al,0
  out 0x08,al			; Clear command register
  mov al,0x58
  out 0x0b,al			; Channel 0: single read, autoinitialize
  mov al,0x00
  out 0x00,al			; Channel 0: start offset 0
  out 0x00,al
  mov al,0xff
  out 0x01,al			; Channel 0: length 65536
  out 0x01,al
  mov al,0x0e
  out 0x0f,al			; Enable channel 0, disable others

; Setup I8254 timer
  mov al,0x36
  out 0x43,al			; Channel 0: lsb+msb, mode 3, binary
  xor al,al
  out 0x40,al			; Channel 0: value 65536
  out 0x40,al
  mov al,0x54
  out 0x43,al			; Channel 1: lsb only, mode 2, binary
  mov al,0x12
  out 0x41,al			; Channel 1: value 18
  mov al,0xb6
  out 0x43,al			; Channel 3: lsb+msb, mode 3, binary
  mov al,0x28
  out 0x42,al			; Channel 3: value 1320
  mov al,0x05
  out 0x42,al

; Setup interrupts
  call init_ivt
  call init_intctl

; Setup BIOS data area
  call init_biosdata

; Enable interrupts
  sti

; Show boot message
  call bootmessage

; Init hardware
  call init_keyboard

; Load and execute boot sector
  int 0x19


; ==========  Setup interrupt vector table
init_ivt:
  push ds
  push bx

; Set all vectors to IRET
  xor bx,bx
  mov ds,bx
init_ivt_fill:
  mov word [bx],int_iret
  mov [bx+2],cs
  add bx,4
  cmp bx,0x300
  jb init_ivt_fill

; Set special vectors
  mov word [4*0x05],int_iret     ; int05 PRINT SCREEN
  mov word [4*0x08],int08_entry  ; int08 IRQ0 TIMER
  mov word [4*0x09],int09_entry  ; int09 IRQ1 KEYBOARD
  mov word [4*0x0a],int_irq_ret  ; int0a IRQ2
  mov word [4*0x0b],int_irq_ret  ; int0b IRQ3 COM2
  mov word [4*0x0c],int_irq_ret  ; int0c IRQ4 COM1
  mov word [4*0x0d],int_irq_ret  ; int0d IRQ5 HARDDISK
  mov word [4*0x0e],int_irq_ret  ; int0e IRQ6 DISKETTE
  mov word [4*0x0f],int_irq_ret  ; int0f IRQ7 LPT1
  mov word [4*0x10],int10_entry  ; int10 VIDEO
  mov word [4*0x11],int11_entry  ; int11 GET EQUIPMENT LIST
  mov word [4*0x12],int12_entry  ; int12 GET MEMORY SIZE
  mov word [4*0x13],int13_entry  ; int13 DISK
  mov word [4*0x14],int_iret     ; int14 SERIAL
  mov word [4*0x15],int15_entry  ; int15 CASETTE
  mov word [4*0x16],int16_entry  ; int16 KEYBOARD
  mov word [4*0x17],int_iret     ; int17 PRINTER
  mov word [4*0x18],int18_entry  ; int18 ROM BASIC
  mov word [4*0x19],int19_entry  ; int19 BOOTSTRAP
  mov word [4*0x1a],int1a_entry  ; int1a TIME
  mov word [4*0x1b],int_iret     ; int1b BREAK HANDLER
  mov word [4*0x1c],int_iret     ; int1c SYSTEM TIMER TICK
  mov word [4*0x1d],int1d_addr   ; int1d VIDEO PARAMETER TABLE
  mov word [4*0x1e],int1e_addr   ; int1e DISKETTE PARAMETERS
  mov word [4*0x1f],int1f_addr   ; int1f GRAPHICS FONT

  pop bx
  pop ds
  ret


; ==========  Setup I8259 interrupt controller
init_intctl:
  ; ICW1: edge triggered, 4-byte vectors, single mode
  mov al,0x13
  out 0x20,al
  ; ICW2: base vector = 8
  mov al,0x08
  out 0x21,al
  ; ICW4: normal-nested mode, buffered mode, manual EOI, 8086 mode
  mov al,0x09
  out 0x21,al
  ; OCW1: enable all interrupts
  mov al,0
  out 0x21,al
  ret


; ==========  Setup BIOS data area
init_biosdata:
  push di

; Clear BIOS data area
  push es
  mov ax,ds
  mov es,ax
  xor ax,ax
  xor di,di
  mov cx,0x80
  cld
  rep stosw
  pop es

; Store initial values
;  mov word [BiosEquip],0x002d		; Store equipment list (int11)
  mov word [BiosMemSize],640		; Store memory size (int12)
  mov word [BiosVCrtBase],CgaCrtReg	; Store CRT controller base port

; Keyboard status
  mov byte [BiosKeyShift1],0
  mov byte [BiosKeyShift2],0
  mov byte [BiosKeyAlt],0
  mov word [BiosKeyBufStart],0x001e
  mov word [BiosKeyBufEnd],0x003e
  mov word [BiosKeyBufRPtr],0x001e
  mov word [BiosKeyBufWPtr],0x001e

  pop di
  ret


; ==========  Initialize keyboard controller
init_keyboard:
  cli
  mov al,0x99
  out 0x63,al
; Read low nibble of S2 switches
  mov al,0x80
  out 0x61,al
  in  al,0x62
  and al,0x0f
  mov byte [BiosEquip],al
; Read high nibble of S2 switches
  mov al,0x88
  out 0x61,al
  in al,0x62
  shl al,1
  shl al,1
  shl al,1
  shl al,1
  or byte [BiosEquip],al
; Enable keyboard
  mov al,0x48
  out 0x61,al
  sti
  ret


; ==========  Print string on screen
printmsg:
  push bx
  push si
printmsg_loop:
  cs lodsb
  and al,al
  jz printmsg_done
  mov bx,7
  mov ah,0x0e
  int 0x10
  jmp short printmsg_loop
printmsg_done:
  pop si
  pop bx
  ret


; ==========  Halt system
halt_system:
  mov si,str_systemhalt
  call printmsg
halt_loop:
  hlt
  jmp short halt_loop


; ==========  Clear screen and show boot message
bootmessage:
  mov ax,3
  int 0x10
  mov si,str_bootmessage
  call printmsg
  ret


; ========================================
;  Generic setup for BIOS ISRs
; ========================================
%define origBX    word [bp]
%define origCX    word [bp+2]
%define origDX    word [bp+4]
%define origSI    word [bp+6]
%define origDI    word [bp+8]
%define origBP    word [bp+10]
%define origAX    word [bp+12]
%define origDS    word [bp+14]
%define origES    word [bp+16]
%define origFlags word [bp+22]
%define origAL    byte [bp+12]
%define origAH    byte [bp+13]
%define origBL    byte [bp]
%define origBH    byte [bp+1]
%define origCL    byte [bp+2]
%define origCH    byte [bp+3]
%define origDL    byte [bp+4]
%define origDH    byte [bp+5]

; Generic setup subroutine for interrupt handlers.
; Out: original registers saved on stack, ds=0x0040, df=0,
;      bp points to original registers (origXX macros)
bios_isr_enter:
  cld
  push ds
  push ax
  push bp
  push di
  push si
  push dx
  push cx
  push bx
  mov bp,sp
  push ax
  mov ax,0x0040
  mov ds,ax
  pop ax
  push word [bp+16]
  mov [bp+16],es
  ret

; Restore state and return from interrupt.
; This is a jump target, not a call subroutine.
bios_isr_exit:
  pop bx
  pop cx
  pop dx
  pop si
  pop di
  pop bp
  pop ax
  pop ds
  pop es
  iret


; ========================================
;  INT 08 - IRQ0 TIMER
; ========================================
int08_entry:
; Setup
  sti
  push ds			; save registers (note that some int1c
  push ax			; ... destroy AX and DX because they expect
  push dx			; ... use to save them here)
  mov ax,0x0040
  mov ds,ax

; Increment jiffy counter
  inc word [BiosTimerCount]	; increment low word
  jnz int08_checkday		; no carry, skip
  inc word [BiosTimerCount+2]	; increment high word
int08_checkday:
  cmp word [BiosTimerCount+2],24 ; reached midnight? (1573040 jiffies)
  jb int08_donetimer		; ... no, skip
  cmp word [BiosTimerCount],176
  jb int08_donetimer		; ... no, skip
  xor ax,ax
  mov [BiosTimerCount],ax	; reset counter
  mov [BiosTimerCount],ax
  mov byte [BiosTimerDayFlag],1	; set midnight flag
int08_donetimer:
  
; Invoke clock handler hook
  int 0x1c

; Ack hardware interrupt
  cli
  mov al,0x20
  out 0x20,al			; send end-of-interrupt

; Return from interrupt
  pop dx
  pop ax
  pop ds
  iret


; ========================================
;  INT 09 - IRQ1 KEYBOARD
; ========================================
int09_entry:
; Setup
  sti
  push ds
  push ax
  push bx
  push dx
  mov ax,0x0040
  mov ds,ax

; Get scancode from keyboard controller
  in  al,0x60			; read scancode
  mov dl,al
  in  al,0x61			; read system control port
  mov ah,al
  or  al,0x80
  out 0x61,al			; set bit 7 to acknowledge scancode
  mov al,ah
  out 0x61,al			; restore value of system control port
  mov al,dl			; AL = scancode

%ifdef DEBUG_KEYS
  ; Dump key event on stdout
  mov dx,0xe601
  push ax
  mov al,'K'
  out dx,al
  pop ax
  mov dx,0xe602
  out dx,al
  push ax
  mov dx,0xe601
  mov al,0x0a
  out dx,al
  pop ax
%endif

; Look up scancode in table
  or al,al
  mov bl,al
  jz int09_done2		; ignore scancode 0
  and bx,0x007f			; strip bit-7
  cmp bl,0x54			; scancode out of range?
  ja int09_done2		; ... yes, ignore
  mov bl,[cs:bx+keyb_scancode]	; get byte from table

; Check for state keys
  or bl,bl			; bit-7 set?
  jns int09_nostate		; ... no, this is a regular key
  and bl,0x7f			; strip bit-7
  test bl,0x0f			; one of lower 4 bits raised?
  jz int09_toggle		; ... no, this is a toggle key

; Handle shift-type state keys (shift, ctrl, alt)
  test al,0x80			; key release event?
  jnz int09_shiftrelease	; ... yes, jump
  or [BiosKeyShift1],bl		; set key down flag
  jmp int09_done

; Handle release of shift-type state keys
int09_shiftrelease:
  not bl
  and [BiosKeyShift1],bl	; clear key down flag
  cmp al,0xb8			; Alt key released?
  jnz int09_done2		; ... no, done
  xor ax,ax
  xchg al,[BiosKeyAlt]		; fetch and flush pending Alt-XXX code
  test al,al			; code was pending?
  jz int09_done2		; ... no, done
  jmp int09_putbuf		; ... yes, store pending code in buffer

; Handle toggle keys (caps, num, scroll)
int09_toggle:
  test al,0x80			; key released?
  jnz int09_togglerelease	; ... yes, jump
  test byte [BiosKeyShift1],0x04 ; ctrl key down?
  jnz int09_ctrltoggle		; ... yes, special combination
  xor [BiosKeyShift1],bl	; toggle key state
  or [BiosKeyShift2],bl		; set toggle key down flag
  jmp int09_done
int09_togglerelease:
  not bl
  and [BiosKeyShift2],bl	; clear toggle key down flag
int09_done2:
  jmp int09_done

; Handle Ctrl-toggle combinations
int09_ctrltoggle:
  cmp al,0x45			; numlock down?
  jz int09_pause		; ... yes, handle ctrl-numlock
  cmp al,0x46			; scrolllock down?
  jz int09_break		; ... yes, handle ctrl-scrollock
  jmp int09_done

; Regulare keys (non-shift, non-toggle)
int09_nostate:
  test al,0x80			; check for key released event
  jz int09_keydown

; Regular key released
  cmp al,0xd2			; Insert key released ?
  jnz int09_done2
  and byte [BiosKeyShift2],0x7f	; clear insert down flag
  jmp int09_done

; Handle Pause (Ctrl-Numlock)
int09_pause:
  test byte [BiosKeyShift2],0x08 ; already in pause state?
  jnz int09_done2		; ... yes, ignore
  cli
  or byte [BiosKeyShift2],0x08	; set pause state flag
  mov al,0x20
  out 0x20,al			; send end-of-interrupt
int09_pauseloop:
  sti
  hlt				; sleep until next interrupt
  cli
  test byte [BiosKeyShift2],0x08 ; still in pause state?
  jnz int09_pauseloop		; ... yes, keep waiting
  jmp int09_exit		; ... no, done waiting

; Handle Break (Ctrl-Scrollock)
int09_break:
  mov bx,[BiosKeyBufStart]
  mov [BiosKeyBufRPtr],bx	; clear keyboard buffer
  mov [BiosKeyBufWPtr],bx
  mov byte [BiosBreakFlag],1	; set break flag
  and byte [BiosKeyShift2],0xf7 ; clear pause state flag
  int 0x1b			; invoke break handler
  xor ax,ax
  jmp int09_putbuf		; and store 0000 in buffer

; Regular key down
int09_keydown:
  test byte [BiosKeyShift2],0x08 ; waiting in pause state?
  jnz int09_unpause		; ... yes, jump
  mov ah,al			; move scancode to AH
  test byte [BiosKeyShift1],0x08 ; check Alt down flag
  jz int09_noalt

; Handle Alt combinations
  cmp al,0x47			; numpad key?
  jae int09_altnumpad		; ... yes, jump
  mov bx,keyb_scanalt
  cs xlatb			; lookup scancode in Alt-table
  cmp ah,0x39			; Alt-Space ?
  jz int09_putbuf2		; ... yes, store as normal space
  or al,al			; zero value in table?
  jz int09_done2		; ... yes, ignore this key
  mov ah,al			; return table value as extended-scancode
  mov al,0			; return ascii=0
int09_putbuf2:
  jmp int09_putbuf

; This keypress ends a pause state
int09_unpause:
  and byte [BiosKeyShift2],0xf7	; clear pause state flag
  jmp int09_done		; and ignore this keypress

; Alt-numpad builds ASCII code
int09_altnumpad:
  cmp al,0x53
  jz int09_altdel		; Delete key down, check Ctrl-Alt-Del
  cmp bl,'9'
  ja int09_done2
  sub bl,'0'
  jb int09_done2
  mov al,[BiosKeyAlt]		; get pending code
  shl al,1
  add bl,al			; bl = new + 2 * pending
  shl al,1
  shl al,1
  add al,bl			; al = 8 * pending + new + 2 * pending
  mov [BiosKeyAlt],al		; store new pending code
  jmp int09_done

; Ctrl-Alt-Del reboots system
int09_altdel:
  test byte [BiosKeyShift1],0x04 ; test Ctrl down flag
  jz int09_done2
  mov word [BiosPostFlag],0x1234 ; do warm reboot
  jmp bootentry

; Handle Ctrl combinations
int09_noalt:
  test byte [BiosKeyShift1],0x04 ; check for Ctrl down
  jz int09_noctrl
  cmp al,0x37			; Ctrl-Printscreen?
  jz int09_ctrlprscr		; ... yes, jump

; Handle remaining Ctrl-combinations through table lookup
  mov bx,keyb_scanctrl
  cs xlatb
  cmp ah,0x03			; Ctrl-2?
  jz int09_putbuf		; ... yes, return ascii=0
  or al,al			; zero value in table?
  jz int09_done2		; ... yes, ignore this key
  cmp ah,0x3b			; function or numpad key?
  jb int09_putbuf		; ... no, store table value as ascii-code
  mov ah,al			; ... yes, store value as extended key code
int09_putextended:
  mov al,0			; return ascii=0
  jmp short int09_putbuf

; Ctrl-Printscreen returns extended key code 0x72
int09_ctrlprscr:
  mov ax,0x7200
  jmp short int09_putbuf

; Handle numpad keys (depending on shift, numlock)
int09_noctrl:
  mov al,bl			; default ascii result in AL
  cmp ah,0x47			; check for numpad key
  jb int09_nonumpad
  cmp al,'-'			; no case conversion for -
  jz int09_putbuf
  cmp al,'+'			; no case conversion for +
  jz int09_putbuf
  test byte [BiosKeyShift1],0x23
  jpo int09_putbuf 		; check shift XOR numlock state
  mov al,0			; no shift/numlock, return ascii=0
  cmp ah,0x52			; handle Insert down
  jnz int09_putbuf
  xor byte [BiosKeyShift1],0x80 ; switch insert state
  or byte [BiosKeyShift2],0x80  ; set insert down flag
  jmp short int09_putbuf

; Handle letters (depending on shift, capslock)
int09_nonumpad:
  cmp al,'a'
  jb int09_noalpha
  cmp al,'z'
  ja int09_noalpha
  test byte [BiosKeyShift1],0x43 ; check shift XOR capslock state 
  jpe int09_putbuf		; no shift/caps, store lowercase
  and al,0x5f			; shift/caps, store upper case
  jmp short int09_putbuf

; Handle non-numpad, non-alpha keys (depending on shift)
int09_noalpha:
  test byte [BiosKeyShift1],0x03 ; check shift state
  jz int09_putbuf		; no shift, store default case

; Handle shift-combinations
  cmp ah,0x37
  jz int09_printscreen		; shift-printscreen is special
  cmp ah,0x0f
  jz int09_putextended		; shift-tab returns ascii=0
  cmp ah,0x3b
  jae int09_shiftfn		; shift-function key
  sub bl,0x20
  jb int09_putbuf		; control char, no case conversion
  mov bh,0
  mov al,[cs:bx+keyb_caseconvert] ; lookup case conversion
  jmp short int09_putbuf

; Shift-Fn returns extended keycode
int09_shiftfn:
  add ah,0x19			; return extended keycode
  mov al,0			; return ascii=0
  jmp short int09_putbuf

; Store word in keyboard buffer
int09_putbuf:
  mov dx,[BiosKeyBufWPtr]
  mov bx,dx
  add dx,2
  cmp dx,[BiosKeyBufEnd]
  jb int09_putbuf_saveptr
  mov dx,[BiosKeyBufStart]
int09_putbuf_saveptr:
  cmp dx,[BiosKeyBufRPtr]
  jz int09_bell
  mov [bx],ax
  mov [BiosKeyBufWPtr],dx

; Ack hardware interrupt
int09_done:
  cli
  mov al,0x20
  out 0x20,al			; send end-of-interrupt

; Return from interrupt
int09_exit:
  pop dx
  pop bx
  pop ax
  pop ds
  iret

; Handle Shift-PrintScreen
int09_printscreen:
  mov al,0x20
  out 0x20,al			; send end-of-interrupt
  int 5				; invoke print-screen handler
  jmp short int09_exit

; Ring bell for buffer overflow
int09_bell:
; TODO : implement bell
  jmp short int09_done

; Map scancode to ASCII value (for regular keys)
; or to a state key bitmask with sign bit set (for state keys).
keyb_scancode:
;           01 Esc 02 1   03 2   04 3   05 4   06 5   07 6
  db 0x00,  0x1b,  0x31,  0x32,  0x33,  0x34,  0x35,  0x36
;    08 7   09 8   0a 9   0b 0   0c -   0d =   0e Bs  0f Tab
  db 0x37,  0x38,  0x39,  0x30,  0x2d,  0x3d,  0x08,  0x09
;    10 q   11 w   12 e   13 r   14 t   15 y   16 u   17 i
  db 0x71,  0x77,  0x65,  0x72,  0x74,  0x79,  0x75,  0x69
;    18 o   19 p   1a [   1b ]   1c Cr  1d Ctr 1e a   1f s
  db 0x6f,  0x70,  0x5b,  0x5d,  0x0d,  0x84,  0x61,  0x73
;    20 d   21 f   22 g   23 h   24 j   25 k   26 l   27 ;
  db 0x64,  0x66,  0x67,  0x68,  0x6a,  0x6b,  0x6c,  0x3b
;    28 '   29 `   2a LSh 2b \   2c z   2d x   2e c   2f v
  db 0x27,  0x60,  0x82,  0x5c,  0x7a,  0x78,  0x63,  0x76
;    30 b   31 n   32 m   33 ,   34 .   35 /   36 RSh 37 *
  db 0x62,  0x6e,  0x6d,  0x2c,  0x2e,  0x2f,  0x81,  0x2a
;    38 Alt 39 Sp  3a Cap 3b F1  3c F2  3d F3  3e F4  3f F5
  db 0x88,  0x20,  0xc0,  0x00,  0x00,  0x00,  0x00,  0x00
;    40 F6  41 F7  42 F8  43 F9  44 F10 45 Num 46 Scr 47 N7
  db 0x00,  0x00,  0x00,  0x00,  0x00,  0xa0,  0x90,  0x37
;    48 N8  49 N9  4a N-  4b N4  4c N5  4d N6  4e N+  4f N1
  db 0x38,  0x39,  0x2d,  0x34,  0x35,  0x36,  0x2b,  0x31
;    50 N2  51 N3  52 N0  53 N.
  db 0x32,  0x33,  0x30,  0x2e

; Map scancode to extended-keycode for Alt-combinations
keyb_scanalt:
;           01 Esc 02 1   03 2   04 3   05 4   06 5   07 6
  db 0x00,  0x00,  0x78,  0x79,  0x7a,  0x7b,  0x7c,  0x7d
;    08 7   09 8   0a 9   0b 0   0c -   0d =   0e Bs  0f Tab
  db 0x7e,  0x7f,  0x80,  0x81,  0x82,  0x83,  0x00,  0x00
;    10 q   11 w   12 e   13 r   14 t   15 y   16 u   17 i
  db 0x10,  0x11,  0x12,  0x13,  0x14,  0x15,  0x16,  0x17
;    18 o   19 p   1a [   1b ]   1c Cr  1d Ctr 1e a   1f s
  db 0x18,  0x19,  0x00,  0x00,  0x00,  0x00,  0x1e,  0x1f
;    20 d   21 f   22 g   23 h   24 j   25 k   26 l   27 ;
  db 0x20,  0x21,  0x22,  0x23,  0x24,  0x25,  0x26,  0x00
;    28 '   29 `   2a LSh 2b \   2c z   2d x   2e c   2f v
  db 0x00,  0x00,  0x00,  0x00,  0x2c,  0x2d,  0x2e,  0x2f
;    30 b   31 n   32 m   33 ,   34 .   35 /   36 RSh 37 *
  db 0x30,  0x31,  0x32,  0x00,  0x00,  0x00,  0x00,  0x00
;    38 Alt 39 Sp  3a Cap 3b F1  3c F2  3d F3  3e F4  3f F5
  db 0x00,  0x20,  0x00,  0x68,  0x69,  0x6a,  0x6b,  0x6c
;    40 F6  41 F7  42 F8  43 F9  44 F10 45 Num 46 Scr
  db 0x6d,  0x6e,  0x6f,  0x70,  0x71,  0x00,  0x00

; Map scancode to ASCII value for Ctrl-combinations
; or to extended-keycode for special Ctrl-combinations
keyb_scanctrl:
;           01 Esc 02 1   03 2   04 3   05 4   06 5   07 6
  db 0x00,  0x1b,  0x00,  0x00,  0x00,  0x00,  0x00,  0x1e
;    08 7   09 8   0a 9   0b 0   0c -   0d =   0e Bs  0f Tab
  db 0x00,  0x00,  0x00,  0x00,  0x1f,  0x00,  0x7f,  0x00
;    10 q   11 w   12 e   13 r   14 t   15 y   16 u   17 i
  db 0x11,  0x17,  0x05,  0x12,  0x14,  0x19,  0x15,  0x09
;    18 o   19 p   1a [   1b ]   1c Cr  1d Ctr 1e a   1f s
  db 0x0f,  0x10,  0x1b,  0x1d,  0x0a,  0x00,  0x01,  0x13
;    20 d   21 f   22 g   23 h   24 j   25 k   26 l   27 ;
  db 0x04,  0x06,  0x07,  0x08,  0x0a,  0x0b,  0x0c,  0x00
;    28 '   29 `   2a LSh 2b \   2c z   2d x   2e c   2f v
  db 0x00,  0x00,  0x00,  0x1c,  0x1a,  0x18,  0x03,  0x16
;    30 b   31 n   32 m   33 ,   34 .   35 /   36 RSh 37 *
  db 0x02,  0x0e,  0x0d,  0x00,  0x00,  0x00,  0x00,  0x00
;    38 Alt 39 Sp  3a Cap 3b F1  3c F2  3d F3  3e F4  3f F5
  db 0x00,  0x20,  0x00,  0x5e,  0x5f,  0x60,  0x61,  0x62
;    40 F6  41 F7  42 F8  43 F9  44 F10 45 Num 46 Scr 47 N7
  db 0x63,  0x64,  0x65,  0x66,  0x67,  0x00,  0x00,  0x77
;    48 N8  49 N9  4a N-  4b N4  4c N5  4d N6  4e N+  4f N1
  db 0x85,  0x84,  0x86,  0x73,  0x87,  0x74,  0x88,  0x75
;    50 N2  51 N3  52 N0  53 N.
  db 0x89,  0x76,  0x8a,  0x8b

; Map ASCII values to corresponding case converted ASCII values
keyb_caseconvert:
; 0x20   !"#$%&'()*+,-./
  db   '       "    <_>?'
; 0x30  0123456789:;<=>?
  db   ')!@#$%^&*( : +  '
; 0x40  @ABCDEFGHIJKLMNO
  db   '                '
; 0x50  PQRSTUVWXYZ[\]^_
  db   '           {|}  '
; 0x60  `
  db   '~'


; ========================================
;  DEFAULT IRQ HANDLER
; ========================================
int_irq_ret:
  push ax
  mov al,0x20
  out 0x20,al			; generic end-of-interrupt
  pop ax
  iret


; ========================================
;  INT 10 - VIDEO
; ========================================
%define int10_exit bios_isr_exit

; ==========  Setup and dispatch to subfunction handler
int10_entry:
  sti
  call bios_isr_enter		; save context and setup registers
  mov bx,0xb800
  mov es,bx			; ES = 0xb800
  mov bl,ah			; BX destroyed, subhandler must use origBX !!
  xor bh,bh
  cmp bl,0x13			; valid subfunction?
  ja  int10_nofunc		; ... no, exit
  shl bx,1
  jmp [cs:bx+int10_subfn_table]	; dispatch to subfunction handler
int10_nofunc:
  jmp int10_exit

; ==========  Subfunction table
int10_subfn_table:
  dw int10_fn00 ; 00 set video mode
  dw int10_exit ; 01 set cursor size
  dw int10_fn02 ; 02 set cursor position
  dw int10_fn03 ; 03 get cursor position and size
  dw int10_exit ; 04 read light pen position
  dw int10_fn05 ; 05 select active display page
  dw int10_fn06 ; 06 scroll window up
  dw int10_fn07 ; 07 scroll window down
  dw int10_fn08 ; 08 read character and attribute
  dw int10_fn09 ; 09 write character and attribute
  dw int10_fn0a ; 0a write character
  dw int10_fn0b ; 0b set color palette
  dw int10_fn0c ; 0c write graphics pixel
  dw int10_fn0d ; 0d read graphics pixel
  dw int10_fn0e ; 0e tty character output
  dw int10_fn0f ; 0f get current video mode
  dw int10_exit ; 10 (ega/vga)
  dw int10_exit ; 11 (ega/vga)
  dw int10_exit ; 12 (ega/vga)
  dw int10_exit ; 13 write string

; ==========  INT 10 - fn00 - SET VIDEO MODE
; In: al=mode,clearflag
int10_fn00:
  mov bl,al
  and bx,0x007f				; BX = videomode
  cmp bl,7				; valid mode?
  ja int10_exit				; ... no, exit
; Store mode number in biosdata
  mov [BiosVMode],bl			; store new mode in biosdata
  mov word [BiosVPageStart],0		; set active page to 0
  mov byte [BiosScreenRows],0x18	; set number of text rows to 24+1
; Store default color register in biosdata
  mov al,0				; AL = black border color
  cmp bl,4				; graphics mode?
  jb int10_fn00_setcolor		; ... no, got final color
  mov al,0x20				; AL = cyan,magenta,white palette
  cmp bl,6				; hires graphics?
  jnz int10_fn00_setcolor		; ... no, got final color
  mov al,0x0f				; AL = white foreground color
int10_fn00_setcolor:
  mov [BiosVColorReg],al		; store color value in biosdata
; Get address of video mode table
  xor ax,ax
  mov es,ax				; ES = 0
  les di,[es: 4 * 0x1d]			; ES:DI = CRT register table
; Get some data from fixed table, not from user defined table
  mov al,[cs:bx+video_param_columns]	; AL = screen width
  xor ah,ah
  mov [BiosVColumns],ax
  mov al,[cs:bx+video_param_crtmode]	; AL = CRT mode byte
  mov [BiosVModeReg],al
  and bl,0xfe				; BX = 2 * (videomode / 2)
  mov dx,[cs:bx+video_param_pagesize]	; DX = page size in bytes
  mov [BiosVPageSize],dx
  shl bx,1				; BX = 16 * (videomode / 2)
  shl bx,1
  shl bx,1
  add di,bx				; ES:DI = CRT registers for this mode
; Reprogram CRT controllor
  cli					; no interrupt while programming CRT
  mov dx,CgaModeReg
  and al,0xf7				; disable video signal
  out dx,al				; write CRT mode register
  xor ah,ah				; start at register 0
int10_fn00_writecrt:
  mov dx,[BiosVCrtBase]
  mov al,ah
  out dx,al				; write CRT register index
  inc dx
  mov al,[es:di]			; get data byte from table
  inc di
  out dx,al				; write CRT register data
  inc ah
  cmp ah,0x10				; write the first 16 registers
  jb int10_fn00_writecrt		; loop until AH == 16
  sti
  mov al,[BiosVColorReg]		; AL = color value
  mov dx,CgaColorReg
  out dx,al				; write CGA color register
; Reset biosdata fields            
  mov bx,BiosVCursorPos			; reset cursor positions
int10_fn00_clearbiosdata:
  mov word [bx],0
  add bx,2
  cmp bx,BiosVCursorPos+0x10		; for 8 video pages
  jb int10_fn00_clearbiosdata
  mov word [BiosVCursorType],0x0607	; default cursor shape
  mov byte [BiosVPageNr],0		; set active page to 0
; Clear video memory
  mov ax,0xb800
  mov es,ax				; ES = 0xb800
  test origAL,0x80			; clear inhibit flag?
  jnz int10_fn00_noclear		; ... yes, skip clearing
  xor bx,bx
  mov ax,0x0720				; write white-on-black space chars
  cmp byte [BiosVMode],4		; graphics mode?
  jb int10_fn00_clearmem		; ... no, start clearing
  xor ax,ax				; write background pixels
int10_fn00_clearmem:
  mov word [es:bx],ax			; clear addresses 0 .. 0x8000
  add bx,2
  cmp bx,0x8000
  jb int10_fn00_clearmem
int10_fn00_noclear:
; Enable video signal
  mov al,[BiosVModeReg]
  mov dx,CgaModeReg
  out dx,al				; write CRT mode register
  jmp int10_exit

; ==========  INT 10 - fn02 - SET CURSOR POSITION
; In: bh=pagenr, dh=row, dl=column
int10_fn02:
  mov bl,origBH
  xor bh,bh
  shl bx,1
  mov [BiosVCursorPos+bx],dx
  cmp bl,[BiosVPageNr]
  jnz int10_fn02_done
int10_fn02_done:
  call int10_setcursor
  jmp int10_exit

; ==========  INT 10 - fn03 - GET CURSOR POSITION AND SIZE
; In:  bh=pagenr
; Out: dh=row, dl=column, ch=startline, cl=endline
int10_fn03:
  mov bl,origBH
  xor bh,bh
  shl bx,1
  mov dx,[BiosVCursorPos+bx]
  mov cx,[BiosVCursorType]
  mov origDX,dx
  mov origCX,cx
  jmp int10_exit

; ==========  INT 10 - fn05 - SELECT ACTIVE DISPLAY PAGE
; In:  al=pagenr
int10_fn05:
; not in graphics mode
  cmp byte [BiosVMode],4
  jnb int10_fn05_exit
; update active page number and start address
  mov [BiosVPageNr],al
  xor ah,ah
  mov cx,[BiosVPageSize]
  mul cx
  mov [BiosVPageStart],ax
  mov bx,ax
  shr bx,1
; update start address in CRT controller
  cli
  mov dx,[BiosVCrtBase]
  mov al,0x0c
  out dx,al
  inc dx
  mov al,bh
  out dx,al
  dec dx
  mov al,0x0d
  out dx,al
  inc dx
  mov al,bl
  out dx,al
  sti
; update cursor position
  call int10_setcursor
int10_fn05_exit:
  jmp int10_exit

; ==========  INT 10 - fn06 - SCROLL WINDOW UP
; In: al=lines, bh=attr, ch=toprow, cl=leftcol, dh=lowrow, dl=rightcol
int10_fn06:
  and al,0x7f
  mov bl,al			; BL = scroll count
  mov bh,origBH			; BH = attr
  call int10_scrollwin		; call scroll subroutine
  jmp int10_exit

; ==========  INT 10 - fn07 - SCROLL WINDOW DOWN
; In: al=lines, bh=attr, ch=toprow, cl=leftcol, dh=lowrow, dl=rightcol
int10_fn07:
  and al,0x7f
  mov bl,al			; BL = scroll count
  mov bh,origBH			; BH = attr
  neg bl			; negative scroll count scrolls down
  call int10_scrollwin		; call scroll subroutine
  jmp int10_exit

; ==========  INT 10 - fn08 - READ CHARACTER AND ATTRIBUTE
; In:  bh=pagenr
; Out: al=character, ah=attribute
int10_fn08:
  mov ah,[BiosVMode]
  cmp ah,4			; graphics mode?
  jnb int10_fn08_graph		; ... yes, jump
; Handle text mode
  xor bh,bh
  mov bl,origBH			; BX = page number
  mov ax,[BiosVPageSize]
  mul bx			; AX = page address
  mov di,ax
  mov ax,[BiosVColumns]		; AX = screen width
  shl bx,1
  mul byte [BiosVCursorPos+bx+1] ; AX = width * row
  add al,[BiosVCursorPos+bx]
  adc ah,0			; AX = width * row + column
  add di,ax
  add di,ax			; DI = character offset
  mov ax,[es:di]		; AL = character; AH = attribute
  mov origAX,ax
  jmp int10_exit
  
int10_fn08_graph:
; Handle graphics mode
  mov cx,[BiosVCursorPos]	; CH = row, CL = column
  call int10_graphcharaddr	; DI = character offset
  sub sp,8			; reserve 8 bytes stack space
  mov bx,sp			; BX = 8 byte buffer
  cmp ah,6			; 640x200 mode?
  jnb int10_fn08_hires		; ... yes, jump
; Fetch character in 320x200 mode
  xor ah,ah
  mov cl,64			; fetch 64 pixels
  or di,1			; start at odd address
int10_fn08_lowresbyte:		; for each byte ...
  mov al,[es:di]		; fetch byte from video memory
  or ah,al			; keep pixel colors seen
  mov dh,al
  shr al,1
  or  al,dh			; AL = data OR (data >> 1)
int10_fn08_lowrespixel:		; for each pixel ...
  shr al,1			; shift two bits out from AL and
  rcr dl,1			; ... shift the first of these into DL
  shr al,1
  dec cl
  test cl,3			; handled 4 pixels?
  jnz int10_fn08_lowrespixel	; ... no, do next pixel 
  xor di,1			; ... yes, switch between odd/even address
  test cl,4			; handled two bytes from scanline?
  jnz int10_fn08_lowresbyte	; ... no, do next byte
  mov [ss:bx],dl		; ... yes, store decoded byte in buffer
  inc bx
  xor di,8192			; switch between odd/even scanlines
  test cl,8			; handled scanline pair?
  jnz int10_fn08_lowresbyte	; ... no, do odd scanline
  add di,80			; ... yes, move to next scanline pair
  or cl,cl			; handled 4 scanline pairs?
  jnz int10_fn08_lowresbyte	; ... no, do next scanline pair
; If we hit any nonzero pixel, store some color value in AH
  mov al,ah
int10_fn08_lowrescolor:	
  mov ah,al
  and ah,3
  shr al,1
  shr al,1
  jnz int10_fn08_lowrescolor
  jmp short int10_fn08_donecopy
int10_fn08_hires:
; Fetch character in 640x200 mode
  mov cl,4			; fetch 4 scanline pairs
int10_fn08_hiresloop:		; for each scanline pair ...
  mov al,[es:di]		; fetch byte from even scanline
  mov ah,[es:di+8192]		; fetch byte from odd scanline
  mov [ss:bx],ax		; store both bytes in buffer
  add bx,2
  add di,80			; move to next scanline pair
  dec cl
  jnz int10_fn08_hiresloop	; loop until CL == 0
  mov ah,1			; color value is always 1
int10_fn08_donecopy:
; Search font tables for character pattern
  sub bx,8			; BX = start of buffer
  mov dx,ss
  mov es,dx			; ES:BX = buffer
  mov dx,cs
  mov ds,dx
  mov si,video_font_tbl		; DS:SI = low character font table
  call int10_scanchartbl	; scan the table
  jz int10_fn08_graphexit	; ... found, jump to end
  xor dx,dx
  mov ds,dx			; DS = 0
  lds si,[4 * 0x1f]		; DS:SI = vector 0x1f = high character font
  call int10_scanchartbl	; scan the table
  jz int10_fn08_foundhigh	; ... found, jump
  xor ax,ax			; ... not found, return zero
  jmp short int10_fn08_graphexit
int10_fn08_foundhigh:
  or al,0x80			; add high bit if found in high font table
int10_fn08_graphexit:
  add sp,8			; release stack buffer
  mov origAX,ax
  jmp int10_exit

; ==========  INT 10 - fn09 - WRITE CHARACTER AND ATTRIBUTE
; In: al=character, bh=pagenr, bl=attribute, cx=count
int10_fn09:
  mov dl,1			; DL = 1 -> enable attribute
int10_fn09_writechars:
  xor bx,bx			; BX = 0
  cmp byte [BiosVMode],4	; graphics mode?
  jnb int10_fn09_nopages	; ... yes, ignore page number
  mov bl,origBH
  shl bx,1			; BX = 2 * page index
int10_fn09_nopages:
  mov cx,[BiosVCursorPos+bx]	; CX = cursor location
  mov bx,origBX
  mov si,origCX			; SI = character count
int10_fn09_charloop:
  mov al,origAL
  call int10_showchar		; show one character
  dec si
  jz int10_fn09_done		; stop if SI == 0
  inc cl			; move to next column
  cmp cl,[BiosVColumns]		; past last screen column?
  jb int10_fn09_charloop	; ... no, print next character
  xor cl,cl			; ... yes, wrap to next row
  inc ch
  jmp short int10_fn09_charloop	; and print next character
int10_fn09_done:
  jmp int10_exit

; ==========  INT 10 - fn0a - WRITE CHARACTER
; In: al=character, bh=pagenr, bl=graphicscolor, cx=count
int10_fn0a:
  xor dl,dl			; DL = 0 -> disable attribute
  jmp short int10_fn09_writechars

; ==========  INT 10 - fn0b - SET COLOR PALETTE
; In: bh=00, bl=color (border color in text mode / foreground color in hires mode)
; In: bh=01, bl=palette (in lowres graphics mode)
int10_fn0b:
  mov bx,origBX
  and bl,0x1f			; 5-bit color value
  mov al,0xe0			; change only low 5 bits in register
  or bh,bh			; subfunction bh=0?
  jz int10_fn0b_setreg		; ... yes, set register
  cmp bh,1			; subfunction bh=1?
  ja int10_fn0b_exit		; ... no, ignore unknown subfunction
  mov al,0xdf			; change only bit 5 in register (palette)
  test bl,1			; which palette?
  mov bl,0x20			; ... set bit 5
  jnz int10_fn0b_setreg
  xor bl,bl			; ... clear bit 5
int10_fn0b_setreg:
  and al,[BiosVColorReg]	; get unchanged bits from old register
  or al,bl			; add changed bits
  mov [BiosVColorReg],al	; store new value
  mov dx,CgaColorReg
  out dx,al			; write new value to color register
int10_fn0b_exit:
  jmp int10_exit

; ==========  INT 10 - fn0c - WRITE GRAPHICS PIXEL
; In: al=color, cx=column, dx=row
int10_fn0c:
  mov ah,[BiosVMode]		; AH = videomode
  cmp ah,4			; graphics mode?
  jb int10_fn0c_exit		; ... no, ignore
  call int10_pixeladdr		; compute pixel address from row,column
  mov dl,al
  shl ax,cl			; shift pixel value and pixel mask
  and al,ah			; mask new pixel value
  test dl,128			; XOR mode?
  jnz int10_fn0c_xor		; ... yes
  not ah
  and ah,[es:bx]		; fetch unchanged bits
  or al,ah			; merge with changed bits
  mov [es:bx],al		; write new value
  jmp short int10_fn0c_exit
int10_fn0c_xor:
  xor [es:bx],al		; do XOR operation
int10_fn0c_exit:
  jmp int10_exit

; ==========  INT 10 - fn0d - READ GRAPHICS PIXEL
; In:  cx=column, dx=row
; Out: al=color
int10_fn0d:
  mov ah,[BiosVMode]		; AH = videomode
  cmp ah,4			; graphics mode?
  jb int10_fn0d_exit		; ... no, ignore
  call int10_pixeladdr		; compute pixel address from row,column
  mov al,[es:bx]		; fetch byte from video memory
  shr al,cl			; unshift and mask one pixel
  and al,ah
  mov origAL,al			; return color value
int10_fn0d_exit:
  jmp int10_exit

; ==========  INT 10 - fn0e - TTY CHARACTER OUTPUT
; In: al=character, bl=graphicscolor
int10_fn0e:
  xor bh,bh
  mov bl,[BiosVPageNr]		; BX = active page number
  shl bx,1
  add bx,BiosVCursorPos		; BX = pointer to cursor position
; Detect control characters
  cmp al,13			; control character?
  ja int10_fn0e_showchar	; ... not, show character
  jz int10_fn0e_cr		; ... carriage return, handle it
  cmp al,10			; linefeed?
  jz int10_fn0e_lf		; ... yes, handle it
  cmp al,7			; bell?
  jz int10_fn0e_bell		; ... yes, handle it
  cmp al,8			; backspace?
  jz int10_fn0e_bs		; ... yes, handle it
int10_fn0e_showchar:
; Show normal character
  push bx
  xor dl,dl			; don't modify attribute
  mov cx,[bx]			; CX = current cursor position
  mov bh,[BiosVPageNr]		; BH = active page index
  mov bl,origBL			; BL = color
  call int10_showchar		; show character
  pop bx
  inc byte [bx]			; move to next column
  mov al,[BiosVColumns]
  cmp [bx],al			; past last screen column?
  jb int10_fn0e_setcursor	; ... no, jump
  mov byte [bx],0		; ... yes, wrap to next row
int10_fn0e_lf:
; Handle linefeed or linewrap
  inc byte [bx+1]		; move to next row
  cmp byte [bx+1],25		; past last screen row?
  jb int10_fn0e_setcursor	; ... no, jump
  dec byte [bx+1]		; ... yes, stay on current row
  mov ax,[BiosVColumns]		; AX = screen width
  mov bl,50
  mul bl			; AX = bytes in whole screen
  mov bx,[BiosVPageStart]
  add bx,ax			; BX = address past end of screen
  mov bh,[es:bx-1]		; BH = attribute of last character on screen
  xor cx,cx			; CH,CL = 0,0
  mov dx,[BiosVColumns]
  dec dl			; DL = last screen column
  mov dh,24			; DH = 24 (last screen row)
  mov bl,1			; BL = 1 (scroll 1 line up)
  call int10_scrollwin		; call scroll subroutine
  jmp short int10_fn0e_setcursor
int10_fn0e_bs:
; Handle backspace
  cmp byte [bx],0		; cursor in first column?
  jz int10_fn0e_setcursor	; ... yes, ignore
  dec byte [bx]			; ... no, move one column to the left
  jmp short int10_fn0e_setcursor
int10_fn0e_cr:
; Handle carriage return
  mov byte [bx],0		; move cursor to first column
int10_fn0e_setcursor:
  call int10_setcursor		; update CRT cursor position
  jmp int10_exit
int10_fn0e_bell:
  call int10_bell		; ring bell
  jmp int10_exit

; ==========  INT 10 - fn0f - GET CURRENT VIDEO MODE
; Out: al=videomode, ah=columns, bh=pagenr
int10_fn0f:
  mov al,[BiosVMode]
  mov ah,[BiosVColumns]
  mov bh,[BiosVPageNr]
  mov origAX,ax
  mov origBH,bh
  jmp int10_exit

; ==========  Set text mode cursor for active page
int10_setcursor:
  push bx
  mov bl,[BiosVPageNr]
  xor bh,bh
  shl bx,1
  mov ax,[BiosVColumns]		; AX = width
  mov cl,[BiosVCursorPos+bx+1]	; CX = row
  xor ch,ch
  mul cx			; AX = row * width
  add al,[BiosVCursorPos+bx]	; AX = row * width + col
  adc ah,0
  add ax,[BiosVPageStart]	; AX += page offset (AX = cursor offset)
  mov cx,ax
  cli
  mov dx,[BiosVCrtBase]
  mov al,0x0e
  out dx,al			; CRT register 14
  inc dx
  mov al,ch			; high byte cursor offset
  out dx,al
  dec dx
  mov al,0x0f			; CRT register 15
  out dx,al
  inc dx
  mov al,cl			; low byte cursor offset
  out dx,al
  sti
  pop bx
  ret

; ==========  Put character in video buffer
; In:  al=character, bh=pagenr, bl=attribute, ch=row, cl=column, dl=useattr
; Out: preserves bx, cx; destroys ax, dx
int10_showchar:
  mov ah,[BiosVMode]
  cmp ah,4			; graphics mode?
  jnb int10_showchar_graph	; ... yes, jump
  mov dh,al			; DH = character
  push cx
  push dx
  mov ax,[BiosVPageSize]	; AX = page size
  mov cl,bh			; CX = page index
  xor ch,ch
  mul cx			; AX = page offset
  pop dx
  pop cx
  push di
  mov di,ax			; DI = page offset
  mov ax,[BiosVColumns]	
  mul ch			; AX = width * row
  add al,cl			; AX = width * row + col
  adc ah,0
  add di,ax			; DI += 2 * AX (DI = character offset)
  add di,ax
  mov [es:di],dh		; write character
  or dl,dl			; also write attribute?
  jz int10_showchar_done	; ... no, jump
  mov [es:di+1],bl		; write attribute
  int10_showchar_done:
  pop di
  ret

; Put character in video buffer in graphics mode
; al=character, bl=color,xorflag, ch=row, cl=column
; preserves bx, cx; destroys ax, dx
int10_showchar_graph:
  mov dx,ax			; DL = character, DH = videomode
  push di
  push si
; Get pointer to font table
  mov ax,cs
  mov ds,ax
  mov si,video_font_tbl		; DS:SI = ascii font table
  test dl,0x80			; is it an ascii character?
  jz int10_showchar_gotfont	; ... yes, jump
  and dl,0x7f			; bring high character in 0..127 range
  xor ax,ax
  mov ds,ax			; DS = 0
  lds si,[4 * 0x1f]		; DS:SI = vector 1F (user defined font)
int10_showchar_gotfont:
; Add offset to current character
  xor ah,ah
  mov al,dl			; AX = character index
  shl ax,1
  shl ax,1
  shl ax,1
  add si,ax			; SI += 8 * char index
; DS:SI now points to the 8x8 character bitmap
  mov ah,dh			; AH = videomode
  call int10_graphcharaddr	; compute videomem address of character
  cmp ah,6			; 640x200 mode?
  jnb int10_showchar_hires	; ... yes, jump
; Handle 320x200 mode
  mov al,bl
  and al,3			; AL = 2-bit pixel color
  mov dh,al
int10_showchar_loresmask:	; repeat the 2-bit pixel color
  or dh,al			; ... 4 times over the 8 bits of DH
  shl al,1			; ... to form a 4-pixel byte pattern
  shl al,1
  jnz int10_showchar_loresmask  ; loop until AL == 0
  mov dl,64			; 64 pixels
  or di,1			; start at odd address
int10_showchar_loresrow:	; for each scanline ...
  lodsb				; load data for one scanline
int10_showchar_lorespixel:	; for each pixel ...
  shr al,1			; expand each bit from the font bitmap
  rcr ah,1			; ... into two bits in AH
  sar ah,1
  dec dl			; loop 4 times to produce one output byte,
  test dl,3			; ... which corresponds to 4 pixels
  jnz int10_showchar_lorespixel
  and ah,dh			; mask with the color pattern
  test bl,128			; xor mode requested?
  jnz int10_showchar_loresxor	; ... yes, jump
  mov [es:di],ah		; write 4 pixels
  jmp short int10_showchar_loresnext
int10_showchar_loresxor
  xor [es:di],ah		; write 4 pixels in xor mode
int10_showchar_loresnext:
  xor di,1			; switch between odd/even addresses
  test dl,4			; done with this scanline?
  jnz int10_showchar_lorespixel	; ... no, do next byte on same scanline
  xor di,8192			; ... yes, switch between odd/even scanline
  test dl,8			; done with this scanline pair?
  jnz int10_showchar_loresrow	; ... no, do next scanline in same pair
  add di,80			; ... yes, move to next scanline pair
  or dl,dl			; done with all pixels?
  jnz int10_showchar_loresrow	; ... no, do next scanline pair
  jmp short int10_showchar_graphdone
int10_showchar_hires:
; Handle 640x200 mode
  mov dl,4			; loop 4 times
int10_showchar_hiresloop:	; for each scanline pair ...
  lodsw				; fetch data for two scanlines
  test bl,128			; XOR mode requested?
  jnz int10_showchar_hiresxor	; ... yes, jump
  mov [es:di],al		; write even scanline
  mov [es:di+8192],ah		; write odd scanline
  jmp short int10_showchar_hiresnext
int10_showchar_hiresxor:
  xor [es:di],al		; write even scanline
  xor [es:di+8192],ah		; write odd scanline
int10_showchar_hiresnext:
  add di,80			; compute address of next scanline pair
  dec dl
  jnz int10_showchar_hiresloop	; loop until DL == 0
int10_showchar_graphdone:
  pop si
  pop di
  mov ax,0x0040
  mov ds,ax			; restore DS = 0x0040
  ret

; ==========  Beep the PC speaker
int10_bell:
; TODO : implement
  ret

; ==========  Scroll a window of the screen up or down
; In:  bl=scrollup, bh=blankattr, ch=uprow, cl=leftcol, dh=lowrow, dl=rightcol
; Out: ax, bx, cx, dx destroyed
int10_scrollwin:
  cld
  push si
  push di
  sub dx,cx			; DH = lowrow - uprow; DL = rightcol - leftcol
  inc dl			; DL = number of columns in window
  mov si,[BiosVColumns]		; SI = character step to next row
  or bl,bl			; scroll up or down?
  js int10_scrollwin_down	; ... down
  jnz int10_scrollwin_up	; ... up
  mov bl,0x7f			; ... zero, set to maximum scroll up
  jmp short int10_scrollwin_up
int10_scrollwin_down:
  neg bl			; BL = abs scroll count
  add ch,dh			; CH = lowrow
  neg si			; SI = character step to previous row
int10_scrollwin_up:
  inc dh			; DH = number of rows in window
  sub dh,bl			; DH = rows in window - scrollcount
  jnb int10_scrollwin_gotcount	; ... no underflow, ok
  add bl,dh			; underflow, set BL = rows in window
  sub dh,dh			; and copy zero rows
int10_scrollwin_gotcount:
  mov ah,[BiosVMode]
  cmp ah,4			; graphics mode?
  jnb int10_scrollgraph		; ... yes, jump

; Handle text mode
; bl=abs_scrollcount, bh=blankattr, ch=firstrow, cl=leftcol
; dl=columns_in_window, dh=rows_to_copy, si=char_step
  mov ax,[BiosVColumns]
  mul ch			; AX = width * firstrow
  add al,cl
  adc ah,0
  mov di,ax			; DI = width * firstrow + leftcol
  shl di,1
  add di,[BiosVPageStart]	; DI = first destination address
  mov ax,si
  imul bl			; AX = char distance between source and dest
  xchg ax,si			; AX = char step between subsequent rows
  shl si,1			; SI = addr distance between source and dest
  add si,di			; SI = first source address
  sub al,dl
  sbb ah,0			; AX = char step - columns in window
  shl ax,1			; AX = address skip to next row
; Copy rows within window
  xor ch,ch
  jmp short int10_scrollwin_nextline
int10_scrollwin_line:		; for each row to copy ...
  mov cl,dl			; CX = characters to copy
  rep es movsw			; copy one text row
  add si,ax			; skip to next row
  add di,ax
int10_scrollwin_nextline:
  dec dh
  jns int10_scrollwin_line	; while DH >= 0
; Clear remaining part of window
  mov si,ax
  mov al,' '			; clear by writing space characters
  mov ah,bh
  jmp short int10_scrollwin_clearnext
int10_scrollwin_clearline:	; for each row to clear ...
  mov cl,dl			; CX = characters to clear
  rep stosw			; clear one text row
  add di,si			; skip to next row
int10_scrollwin_clearnext:
  dec bl
  jns int10_scrollwin_clearline	; while BL >= 0
  pop di
  pop si
  ret

; Handle graphics mode
; bl=abs_scrollcount, ch=firstrow, cl=leftcol
; dl=columns_in_window, dh=rows_to_copy, si=char_step
int10_scrollgraph:
  cmp ah,6			; 640x200 mode?
  jnb int10_scrollgraph_hires	; ... yes, 1 byte per text column
  shl dl,1			; DL = bytes per window row
  shl cl,1			; CL = 2 * leftcol
int10_scrollgraph_hires:
  xor ah,ah
  mov al,ch			; AX = firstrow
  mov di,ax
  shl ax,1
  shl ax,1
  add di,ax			; DI = 5 * firstrow
  xor ah,ah
  mov al,cl			; AX = (1 or 2) * leftcol
  mov cl,6
  shl di,cl			; DI = 320 * firstrow
  add di,ax			; DI = first destination address (scroll up)
  or si,si
  mov ax,80			; AX = addr step to next scanline pair
  or si,si			; scroll direction?
  jns int10_scrollgraph_up	; ... scroll up, ok
  neg ax			; AX = addr step to next scanline pair
  add di,3*80			; DI = first destination address
int10_scrollgraph_up:
  mov si,ax
  imul bl
  xchg ax,si			; AX = addr step to next scanline pair
  shl si,1
  shl si,1			; SI = addr distance between source and dest
  add si,di			; SI = first source address
  sub al,dl			; AX -= bytes to copy per scanline
  sbb ah,32			; AX = address skip between scanline pairs
  shl dh,1
  shl dh,1			; DH = scanline pairs to copy
; Copy scanlines within window
  xor ch,ch
  jmp short int10_scrollgraph_nextline
int10_scrollgraph_line:		; for each scanline pair to copy ...
  mov cl,dl			; CX = bytes to copy
  rep es movsb			; copy even scanline
  mov cl,dl			; CX = bytes to copy
  sub di,si			; DI = addr distance between dest and source
  sub si,cx
  or si,8192			; SI = odd source scanline
  add di,si			; DI = odd destination scanline
  rep es movsb			; copy odd scanline
  add si,ax			; SI = next even source scanline
  add di,ax			; DI = next even destination scanline
int10_scrollgraph_nextline:
  dec dh
  jns int10_scrollgraph_line	; while DH >= 0
; Clear remaining part of window
  mov si,ax
  xor al,al			; clear by writing black pixels
  shl bl,1
  shl bl,1			; BL = scanline pairs to clear
  jmp short int10_scrollgraph_clearnext
int10_scrollgraph_clearline:	; for each scanline pair to clear ...
  mov cl,dl			; CX = bytes to clear
  rep stosb			; clear even scanline
  mov cl,dl			; CX = bytes to clear
  sub di,cx			; DI = even scanline
  or di,8192			; DI = odd scanline
  rep stosb			; clear odd scanline
  add di,si			; skip to next scanline pair
int10_scrollgraph_clearnext:
  dec bl
  jns int10_scrollgraph_clearline ; while BL >= 0
  pop di
  pop si
  ret

; ==========  Compute address and bitmask of a graphics pixel
; In:  ah=videomode, cx=column, dx=row
; Out: bx=address, ah=bitmask_noshift, cl=shiftcount, al preserved, dx destroyed
int10_pixeladdr:
; Compute start address of scanline
  xor bx,bx
  test dl,1			; odd scanline?
  jz int10_pixeladdr_even	; ... no, jump
  mov bh,32			; ... yes, set BX=8192
int10_pixeladdr_even:
  and dl,0xfe
  shl dx,1
  shl dx,1
  shl dx,1
  add bx,dx			; BX += 16 * scanline
  shl dx,1
  shl dx,1
  add bx,dx			; BX += 64 * scanline (BX=80*scanline)
; Check video mode
  mov dx,cx			; DX = column
  not cl			; CL = -(column+1)
  cmp ah,6			; 640x200 mode?
  jnb int10_pixeladdr_hires	; ... yes, jump
; Compute pixel address and bitmask in 320x200 mode
  shr dx,1
  shr dx,1
  add bx,dx			; BX += column / 4
  mov ah,3			; mask = 3 bits per pixel
  and cl,ah
  add cl,cl                     ; shiftcount = 6 or 4 or 2 or 0
  jmp short int10_pixeladdr_done
int10_pixeladdr_hires:
; Compute pixel address and bitmask in 640x200 mode
  shr dx,1
  shr dx,1
  shr dx,1
  add bx,dx			; BX += column / 8
  mov ah,1			; mask = 1 bit per pixel
  and cl,7			; shiftcount = 7 downto 0 
int10_pixeladdr_done:
  ret

; ==========  Compute address of character in graphics mode
; In:  ah=videomode, ch=row, cl=column
; Out: di=address, cx preserved, dx destroyed
int10_graphcharaddr:
; compute address of text row in video memory
  xor dl,dl
  mov dh,ch
  mov di,dx			; DI = 256 * row
  shr dx,1
  shr dx,1
  add di,dx			; DI += 64 * row (DI=4*80*row)
  xor dh,dh
  mov dl,cl
  add di,dx			; DI += column (DI=4*80*row+8*column/8)
  cmp ah,6
  jz int10_graphcharaddr_done
  add di,dx			; DI += column (DI=4*80*row+8*column/4)
int10_graphcharaddr_done:
  ret

; ==========  Scan 8x8 font table for character pattern
; In:  ds:si=table, es:bx=pattern
; Out: al=character, zf=success, ah preserved, si destroyed, di destroyed
int10_scanchartbl:
  mov dx,si
  add dx,128*8			; DX = past end of table
  mov al,127			; AL = 127
  xor ch,ch
int10_scanchartbl_loop:
  sub dx,8			; move backwards in table
  mov si,dx			; SI = current table entry
  mov di,bx			; DI = pattern
  mov cl,4
  repe cmpsw			; compare 8 bytes
  jz int10_scanchartbl_done	; ... match, return ZF=1
  dec al			; no match, next character
  jns int10_scanchartbl_loop	; try again if AL >= 0
  mov al,0			; not found, return ZF=0, AL=0
int10_scanchartbl_done:
  ret


; ========================================
;  INT 11 - GET EQUIPMENT LIST
; ========================================
int11_entry:
  push es
  mov ax,0x0040
  mov es,ax
  mov ax,[es:BiosEquip]
  pop es
  iret


; ========================================
;  INT 12 - GET MEMORY SIZE
; ========================================
int12_entry:
  push es
  mov ax,0x0040
  mov es,ax
  mov ax,[es:BiosMemSize]
  pop es
  iret


; ========================================
;  INT 13 - DISK
; ========================================
int13_entry:
; Stub which calls the native int13 handler in the simulator
  push ax
  mov ax,0xfe13
  int 0xe6
  pop ax
  iret


; ========================================
;  INT 15 - CASETTE
; ========================================
; Not supported; return AH=0x86 and set carry flag
int15_entry:
  push bp
  mov bp,sp
  or word [bp+6],FlagCF
  pop bp
  mov ah,0x86
  iret


; ========================================
;  INT 16 - KEYBOARD
; ========================================
%define int16_exit bios_isr_exit

; INT 16 - dispatch subfunctions
int16_entry:
  sti
  call bios_isr_enter
  cmp ah,0
  jz int16_fn00
  cmp ah,1
  jz int16_fn01
  cmp ah,2
  jz int16_fn02
  jmp int16_exit

; INT 16 - subfunction 00 - READ KEYBOARD CHARACTERS
; Out: ah=scancode, al=ascii
int16_fn00:
; wait until a scancode is present
  cli
  mov bx,[BiosKeyBufRPtr]
  cmp bx,[BiosKeyBufWPtr]
  jnz int16_fn00_gotkey
  sti
  hlt
  jmp short int16_fn00
int16_fn00_gotkey:
; a scancode is waiting in the buffer
  mov ax,[bx]
  add bx,2
  cmp bx,[BiosKeyBufEnd]
  jb int16_fn00_saveptr
  mov bx,[BiosKeyBufStart]
int16_fn00_saveptr:
  mov [BiosKeyBufRPtr],bx
  mov origAX,ax
  and origFlags,(~FlagZF)
  jmp int16_exit

; INT 16 - subfunction 01 - READ KEYBOARD STATUS
; Out: ah=scancode, al=ascii, zf=status
int16_fn01:
  cli
  mov bx,[BiosKeyBufRPtr]
  mov ax,[bx]
  cmp bx,[BiosKeyBufWPtr]
  sti
  jnz int16_fn01_gotkey
  or  origFlags,FlagZF
  jmp int16_exit
int16_fn01_gotkey:
  mov origAX,ax
  and origFlags,(~FlagZF)
  jmp int16_exit

; INT 16 ; subfunction 02 - READ KEYBOARD SHIFT STATUS
; Out: al=shiftstatus
int16_fn02:
  mov al,[BiosKeyShift1]
  mov origAL,al
  jmp int16_exit


; ========================================
;  INT 18 - ROM BASIC
; ========================================
int18_entry:
  mov si,str_norombasic
  call printmsg
  jmp halt_system


; ========================================
;  INT 19 - BOOTSTRAP
; ========================================
int19_entry:

; Set stack at 0030:0100, clear segment registers
  mov ax,0x0030
  mov ss,ax
  mov sp,0x0100
  xor ax,ax
  push ax
  mov es,ax
  mov ds,ax
  sti

; Try reading floppy bootsector
  mov bx,0x7c00
  mov cx,1
  xor dx,dx
  mov ax,0x0201
  int 0x13
  jnc int19_boot

; Try reading harddisk bootsector
  mov cx,1
  mov dx,0x0080
  mov ax,0x0201
  int 0x13
  jnc int19_boot

; Try ROM BASIC
  int 0x18
  
int19_boot:
  jmp 0x0000:0x7c00


; ========================================
;  INT 1A - TIME
; ========================================
int1a_entry:
  push ds
  push ax
  mov ax,0x0040
  mov ds,ax
  pop ax

; INT 1A - dispatch subfunctions
  cmp ah,1			; ah = 01 ?
  jz int1a_fn01
  or ah,ah			; ah = 00 ?
  jnz int1a_exit		; ... no, unknown subfunction

; INT 1A - subfunction 00 - GET SYSTEM TIME
; Out: cx:dx = jiffies since midnight, al = midnight flag
int1a_fn00:
  mov dx,[BiosTimerCount]
  mov cx,[BiosTimerCount+2]
  xor al,al
  xchg al,[BiosTimerDayFlag]
  jmp short int1a_exit

; INT 1A - subfunction 01 - SET SYSTEM TIME
; In: cx:dx = jiffies since midnight
int1a_fn01:
  mov [BiosTimerCount],dx
  mov [BiosTimerCount+2],cx

; exit interrupt
int1a_exit:
  pop ds
  iret
 

; ==========  String table
str_bootmessage:	db 'Retro BIOS',13,10,'Booting...',13,10,13,10,0
str_norombasic:		db 'NO ROM BASIC',13,10,0
str_systemhalt:		db 'SYSTEM HALTED',13,10,0


; ================================================
;  INT 1E - DISKETTE PARAMETERS (at F000:EFC7)
; ================================================
JUMPTO(0xefc7)
int1e_addr:

; TODO : these values are for 5.25 360k floppies, it must be made
;        configurable somehow
diskparam_tbl:
  db 0xaf, 0x02			; steprate / head unload; head load / non-dma
  db 0x25, 0x02			; motor off time; 512 bytes per sector
  db 0x09, 0x2a			; sectors per track; sector gap
  db 0xff, 0x50			; data length (???); format gap length
  db 0xf6, 0x0f			; format filler; head settle time
  db 0x08			; motor start time


; ================================================
;  INT 1D - VIDEO PARAMETER TABLES (at F000:F0A4)
; ================================================
JUMPTO(0xf0a4)
int1d_addr:

video_param_tbl:
; CRT registers for 40x25 text mode
  db 0x38, 0x28, 0x2d, 0x0a, 0x1f, 0x06, 0x19, 0x1c
  db 0x02, 0x07, 0x06, 0x07, 0x00, 0x00, 0x00, 0x00
; CRT registers for 80x25 text mode
  db 0x71, 0x50, 0x5a, 0x0a, 0x1f, 0x06, 0x19, 0x1c
  db 0x02, 0x07, 0x06, 0x07, 0x00, 0x00, 0x00, 0x00
; CRT registers for 320x200 graphics mode
  db 0x38, 0x28, 0x2d, 0x0a, 0x7f, 0x06, 0x64, 0x70
  db 0x02, 0x01, 0x06, 0x07, 0x00, 0x00, 0x00, 0x00
; CRT registers for 640x200 graphics mode
  db 0x38, 0x28, 0x2d, 0x0a, 0x7f, 0x06, 0x64, 0x70
  db 0x02, 0x01, 0x06, 0x07, 0x00, 0x00, 0x00, 0x00

; Page sizes
video_param_pagesize:
  dw 0x0800	; 40x25 text
  dw 0x1000	; 80x25 text
  dw 0x4000	; 320x200 graphics
  dw 0x4000	; 640x200 graphics

; Text columns per video mode
video_param_columns:
  db 0x28, 0x28, 0x50, 0x50, 0x28, 0x28, 0x50, 0x50

; CRT controller mode byte per video mode
video_param_crtmode:
  db 0x2c, 0x28, 0x2d, 0x29, 0x2a, 0x2e, 0x1e, 0x29


; ===========================================
;  INT 1F - 8x8 font for characters 128..255
; ===========================================
int1f_addr:
; This font comes from the fntcol16.zip package (c) by Joseph Gil 
; found at ftp://ftp.simtel.net/pub/simtelnet/msdos/screen/fntcol16.zip
  incbin "vga-rom.f08",1024,1024


; =============================================
;   F000:FA6E - 8x8 font for characters 0..127
; =============================================
JUMPTO(0xfa6e)
video_font_tbl:
; This font comes from the fntcol16.zip package (c) by Joseph Gil 
; found at ftp://ftp.simtel.net/pub/simtelnet/msdos/screen/fntcol16.zip
  incbin "vga-rom.f08",0,1024


; ========================================
;   F000:FF53 - IRET instruction
; ========================================
JUMPTO(0xff53)
int_iret:
  iret


; ========================================
;  F000:FFF0 - Reboot vector
; ========================================
JUMPTO(0xfff0)
reboot:
  jmp 0xf000:bootentry
JUMPTO(0xfff5)
biosdate:
  db '08/16/82'
  db 0
  db 0xfe
  db 0

; ========================================
;  END ROM BIOS
; ========================================
