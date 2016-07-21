MS-DOS 6.2 Boot Sector

7C00                     JMP     START
7C02                     NOP                                        

7C03 bsOEM_NAME            DB      'MSDOS5.0'      ;  8 bytes
7C0B bsBYTES_PER_SECTOR    DW      ?
7C0D bsSECTORS_PER_CLUSTER DB      ?
7C0E bsRESERVED_SECTORS    DW      ?
7C10 bsFAT_COPIES          DB      ?
7C11 bsROOT_DIR_ENTRIES    DW      ?
7C13 bsTOTAL_DISK_SECTORS  DW      ?
7C15 bsMEDIA_DESCRIPTOR    DB      ?
7C16 bsSECTORS_PER_FAT     DW      ?
7C18 bsSECTORS_PER_TRACK   DW      ?
7C1A bsSIDES               DW      ?
7C1C bsHIDDEN_SECTORS_HIGH DW      ?
7C1E bsHIDDEN_SECTORS_LOW  DW      ?
7C20 bsTOTAL_NUM_SECTORS   DD      ?
7C24 bsPHYS_DRIVE_NUMBER_1 DB      ?
7C25 bsPHYS_DRIVE_NUMBER_2 DB      ?
7C26 bsBOOT_RECORD_SIG     DB      29h
7C27 bsVOL_SERIAL_NUM      DD      1F61A800h
7C2B bsVOLUME_LABEL        DB      'NO NAME    '   ; 11 bytes
7C36 bsFILE_SYSTEM_ID      DB      'FAT16   '      ;  8 bytes

[========================================================================]
     Disk Parameter Block

     The DPB is located in the ROM BIOS at the address pointed to by 0078h.
     The 11 bytes starting from START are overwritten at COPY_DPB with the
     DPB (7C3E-7C48).  This is what the area looks like *after* the copy
     at COPY_DPB:
[========================================================================]
7C3E dpbCONTROL_TIMERS     DW      ?
7C40 dpbMOTOR_OFF_DELAY    DB      ?
7C41 dpbBYTES_PER_SECTOR   DB      ?
7C42 dpbSECTORS_PER_TRACK  DB      ?
7C43 dpbGAP_LENGTH         DB      ?
7C44 dpbDATA_LENGTH        DB      ?
7C45 dpbFORMAT_GAP_LENGTH  DB      ?
7C46 dpbFORMAT_FILLER_BYTE DB      ?
7C47 dpbHEAD_SETTLE_TIME   DB      ?
7C48 dpbMOTOR_START_TIME   DB      ?

[========================================================================]
     Following the copy of the DPB, more information is copied over
     previously existing code:
[========================================================================]
7C49 cpbsHIDDEN_SECTORS_HIGH DW      ?
7C4B cpbsHIDDEN_SECTORS_LOW  DW      ?

7C4D                         DB      ?
7C4E                         DB      ?
7C4F                         DB      ?

7C50 cpbsHIDDEN_SECTORS_HIGH DW      ?
7C52 cpbsHIDDEN_SECTORS_LOW  DW      ?

[========================================================================]
     Here is the start of the boot sector code.  Note that the first 11
     bytes will be destroyed later on as described above.
[========================================================================]
7C3E START               CLI                     ; Disable interrupts
7C3F                     XOR        AX,AX        ; AX=0000
7C41                     MOV        SS,AX        ; SS=0000
7C43                     MOV        SP,7C00      ; SP grows in decrements
7C46                     PUSH       SS                                 
7C47                     POP        ES           ; ES=0000
7C48                     MOV        BX,0078      ; The address of the ROM
                                                 ; BIOS disk table is 78h.
                                                 ; (INT 18h).  ROM routine
                                                 ; copies this address during
                                                 ; cold boot initialization.
7C4B                     SS:                                           
7C4C                     LDS        SI,[BX]      ; SI points to ROMBIOS table
                                                 ; The source for the copy
7C4E                     PUSH       DS                                 
7C4F                     PUSH       SI                                 
7C50                     PUSH       SS                                 
7C51                     PUSH       BX                                 
7C52                     MOV        DI,START     ; Address of destination
7C55                     MOV        CX,000B      ; Size of area to copy
                                                 ; (Disk parameters)
7C58                     CLD                     ; Set direction flag to inc
7C59 COPY_DPB            REPZ                    ; Move 11 bytes from the
                                                 ; disk parameter area to
                                                 ; overlap with the start
                                                 ; of the code at 7D3E
                                                 ; (save space?)
7C5A                     MOVSB                                         

7C5B                     PUSH       ES                                 
7C5C                     POP        DS           ; DS=0000
7C5D                     MOV        BYTE PTR [DI-02],0F
                                                 ; At this point, DI points
                                                 ; to 7C49, one byte after
                                                 ; the last thing copied.
                                                 ; Destination operand is
                                                 ; dpbHEAD_SETTLE_TIME.
7C61                     MOV        CX,bsSECTORS_PER_TRACK
7C65                     MOV        [DI-07],CL   ; Destination operand is
                                                 ; dpbSECTORS_PER_TRACK.
7C68                     MOV        [BX+02],AX   ; Destination operand is
                                                 ; dpbMOTOR_OFF_DELAY.
7C6B                     MOV        WORD PTR [BX],START
7C6F                     STI                     ; The code at 7C6B installs
                                                 ; the new Int 1E into the
                                                 ; interrupt table at
                                                 ; 0000:0078. At 7C68, AX is
                                                 ; 0. START is the offset
                                                 ; for the new INT 1E.
7C70                     INT        13           ; Reset drives (AX=0000)
7C72                     JB         ERROR_IN_BOOT_1
7C74                     XOR        AX,AX                              
7C76                     CMP        bsTOTAL_DISK_SECTORS,AX
7C7A                     JZ         LOOP_1

7C7C                     MOV        CX,bsTOTAL_DISK_SECTORS
7C80                     MOV        bsTOTAL_NUM_SECTORS,CX                          
7C84 LOOP_1              MOV        AL,bsFAT_COPIES                          
7C87                     MUL        WORD PTR bsSECTORS_PER_FAT
7C8B                     ADD        AX,bsHIDDEN_SECTORS_HIGH
7C8F                     ADC        DX,bsHIDDEN_SECTORS_LOW
7C93                     ADD        AX,bsRESERVED_SECTORS
7C97                     ADC        DX,+00                             
7C9A                     MOV        [7C50],AX                          
7C9D                     MOV        [7C52],DX                          
7CA1                     MOV        [7C49],AX                          
7CA4                     MOV        [7C4B],DX                          
7CA8                     MOV        AX,0020                            
7CAB                     MUL        WORD PTR bsROOT_DIR_ENTRIES
7CAF                     MOV        BX,bsBYTES_PER_SECTOR
7CB3                     ADD        AX,BX                              
7CB5                     DEC        AX                                 
7CB6                     DIV        BX                                 
7CB8                     ADD        [7C49],AX                          
7CBC                     ADC        WORD PTR [7C4B],+00                
7CC1                     MOV        BX,0500      ; Buffer for root directory
7CC4                     MOV        DX,[7C4B]                          
7CC8                     MOV        AX,[7C49]                          
7CCB                     CALL       CALCULATE

7CCE                     JB         ERROR_IN_BOOT_1                               
7CD0                     MOV        AL,01                              
7CD2                     CALL       READ_SECTOR
7CD5                     JB         ERROR_IN_BOOT_1 ; Error?  Print message                                                     ; and reboot.
7CD7                     MOV        DI,BX
7CDC                     MOV        SI,OFFSET FILE_IO_SYS                            
7CDF                     REPZ
7CE0                     CMPSB
7CE1                     JNZ        ERROR_IN_BOOT_1 ; First file in root dir
                                                    ; is not IO.SYS?  Print
                                                    ; error.
7CE3                     LEA        DI,[BX+20]                         
7CE6                     MOV        CX,000B      ; 11 characters in DOS
                                                 ; filename.
7CE9                     REPZ                                          
7CEA                     CMPSB                   ; Is second file in root
                                                 ; MSDOS.SYS?
7CEB                     JZ         LOOP_2       ; Yes?  Then continue on.
7CED ERROR_IN_BOOT_1     MOV        SI,OFFSET NON_SYSTEM_DISK
7CF0                     CALL       WRITE_STRING                               
7CF3                     XOR        AX,AX                              
7CF5                     INT        16                                 
7CF7                     POP        SI                                 
7CF8                     POP        DS                                 
7CF9                     POP        [SI]                               
7CFB                     POP        [SI+02]                            
7CFE                     INT        19                                 

7D00 ERROR_IN_BOOT_2     POP        AX                                 
7D01                     POP        AX                                 
7D02                     POP        AX                                 
7D03                     JMP        ERROR_IN_BOOT_1
7D05 LOOP_2              MOV        AX,[BX+1A]                         
7D08                     DEC        AX                                 
7D09                     DEC        AX                                 
7D0A                     MOV        BL,SECTORS_PER_CLUSTER
7D0E                     XOR        BH,BH                              
7D10                     MUL        BX                                 
7D12                     ADD        AX,[7C49]                          
7D16                     ADC        DX,[7C4B]                          
7D1A                     MOV        BX,0700      ; DOS loading buffer
7D1D                     MOV        CX,0003                            
7D20 LOOP_3              PUSH       AX                                 
7D21                     PUSH       DX                                 
7D22                     PUSH       CX                                 
7D23                     CALL       CALCULATE                               
7D26                     JB         ERROR_IN_BOOT_2                               
7D28                     MOV        AL,01                              
7D2A                     CALL       READ_SECTOR                               
7D2D                     POP        CX                                 
7D2E                     POP        DX                                 
7D2F                     POP        AX                                 
7D30                     JB         ERROR_IN_BOOT_1
7D32                     ADD        AX,0001                            
7D35                     ADC        DX,+00                             
7D38                     ADD        BX,BYTES_PER_SEC
7D3C                     LOOP       LOOP_3                               
7D3E                     MOV        CH,MEDIA_DESCRIPTOR
7D42                     MOV        DL,PHYS_DRIVE_NUMBER_1
7D46                     MOV        BX,[7C49]                          
7D4A                     MOV        AX,[7C4B]                          
7D4D                     JMP        0070:0000    ; Transfer to ROM BIOS

[========================================================================]
     Procedure     WRITE_STRING
[========================================================================]
     Parameters:
                   SI:  Address of string to write

7D52 WRITE_STRING        LODSB                                         
7D53                     OR         AL,AL                              
7D55                     JZ         RETURN_FROM_2
7D57                     MOV        AH,0E                              
7D59                     MOV        BX,0007                            
7D5C                     INT        10                                 
7D5E                     JMP        WRITE_STRING                               

[========================================================================]
     Procedure     CALCULATE

     This procedure probably translates the sector numbers into physical
     addresses for the low level BIOS functions.
[========================================================================]
7D60 CALCULATE           CMP        DX,SECTORS_PER_TRACK
7D64                     JNB        RETURN_FROM_1
7D66                     DIV        WORD PTR SECTORS_PER_TRACK
7D6A                     INC        DL                                 
7D6C                     MOV        [7C4F],DL                          
7D70                     XOR        DX,DX                              
7D72                     DIV        WORD PTR SIDES
7D76                     MOV        PHYS_DRIVE_NUMBER_2,DL
7D7A                     MOV        [7C4D],AX                          
7D7D                     CLC                                           
7D7E                     RET                                           

7D7F RETURN_FROM_1       STC                                           
7D80 RETURN_FROM_2       RET                                           

[========================================================================]
     Procedure     READ_SECTOR
[========================================================================]
7D81 READ_SECTOR         MOV        AH,02        ; 02h is ReadSector .
7D83                     MOV        DX,[7C4D]    ; DH is head/side number.
                                                 ; DL is drive number.
                                                 ; (Bit 7 of DL set for HD)
7D87                     MOV        CL,06        ; CL is sector number.
7D89                     SHL        DH,CL        ; Multiply DH (number of
                                                 ; heads) by 6.
7D8B                     OR         DH,[7C4F]
7D8F                     MOV        CX,DX
7D91                     XCHG       CH,CL
7D93                     MOV        DL,bsPHYS_DRIVE_NUMBER_1
7D97                     MOV        DH,bsPHYS_DRIVE_NUMBER_2
7D9B                     INT        13           ; ReadSector
7D9D                     RET

7D9E NON_SYSTEM_DISK     DB      13,10
7DA0                     DB      'Non-System disk or disk error'
7DBD                     DB      13,10
7DBF                     DB      'Replace and press any key when ready'
7DE3                     DB      13,10,0
7DE6 FILE_IO_SYS         DB      'IO      SYS'
7DF1 FILE_MSDOS_SYS      DB      'MSDOS   SYS'
7DFC                     DB      0,0,55,AA
