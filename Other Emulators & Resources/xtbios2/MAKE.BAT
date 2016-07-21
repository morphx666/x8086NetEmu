@echo off

set floppy=0
set scsi=0
set ide=0

if exist xtbios.obj del xtbios.obj
if exist xtbios.exe del xtbios.exe

@echo *******************************************************************************
@echo Assembling BIOS
@echo *******************************************************************************
tools\tasm xtbios.asm,xtbios.obj
if errorlevel 1 goto errasm
if not exist xtbios.obj goto errasm

@echo *******************************************************************************
@echo Linking BIOS
@echo *******************************************************************************
tools\warplink /wn xtbios.obj,xtbios.exe
del xtbios.obj
if not exist xtbios.exe goto errlink

@echo *******************************************************************************
@echo Building ROM Images
@echo *******************************************************************************

tools\exe2rom /8 xtbios.exe xtbios.bin
del xtbios.exe

if exist test\picoxt.exe tools\inject /70D0 xtbios.bin test\picoxt.exe

if not exist eproms       mkdir eproms
if not exist eproms\2764  mkdir eproms\2764
if not exist eproms\27128 mkdir eproms\27128
if not exist eproms\27256 mkdir eproms\27256
if not exist eproms\27512 mkdir eproms\27512
if not exist eproms\split mkdir eproms\split

if %floppy%==1 tools\romfill /8 images\fdc344.bin  eproms\2764\fdc344.rom
if %scsi%==1   tools\romfill /8 images\tmc850m.bin eproms\2764\tmc850m.rom
if %ide%==1    tools\romfill /8 images\wdbios.bin  eproms\2764\wdbios.rom
tools\romfill /8 xtbios.bin         eproms\2764\xtbios.rom
tools\romfill /8 images\basicc11.f6 eproms\2764\basicf6.rom
tools\romfill /8 images\basicc11.f8 eproms\2764\basicf8.rom
tools\romfill /8 images\basicc11.fa eproms\2764\basicfa.rom
tools\romfill /8 images\basicc11.fc eproms\2764\basicfc.rom
if %floppy%==0 del eproms\2764\fdc344.rom 2>nul
if %scsi%==0   del eproms\2764\tmc850m.rom 2>nul
if %ide%==0    del eproms\2764\wdbios.rom 2>nul

if %floppy%==1 tools\romfill /16 images\fdc344.bin  eproms\27128\fdc344.rom
if %scsi%==1   tools\romfill /16 images\tmc850m.bin eproms\27128\tmc850m.rom
if %ide%==1    tools\romfill /16 images\wdbios.bin  eproms\27128\wdbios.rom
tools\romfill /16 xtbios.bin         eproms\27128\xtbios.rom
tools\romfill /16 images\basicc11.f6 eproms\27128\basicf6.rom
tools\romfill /16 images\basicc11.f8 eproms\27128\basicf8.rom
tools\romfill /16 images\basicc11.fa eproms\27128\basicfa.rom
tools\romfill /16 images\basicc11.fc eproms\27128\basicfc.rom
if %floppy%==0 del eproms\27128\fdc344.rom 2>nul
if %scsi%==0   del eproms\27128\tmc850m.rom 2>nul
if %ide%==0    del eproms\27128\wdbios.rom 2>nul

if %floppy%==1 tools\romfill /32 images\fdc344.bin  eproms\27256\fdc344.rom
if %scsi%==1   tools\romfill /32 images\tmc850m.bin eproms\27256\tmc850m.rom
if %ide%==1    tools\romfill /32 images\wdbios.bin  eproms\27256\wdbios.rom
tools\romfill /32 xtbios.bin         eproms\27256\xtbios.rom
tools\romfill /32 images\basicc11.f6 eproms\27256\basicf6.rom
tools\romfill /32 images\basicc11.f8 eproms\27256\basicf8.rom
tools\romfill /32 images\basicc11.fa eproms\27256\basicfa.rom
tools\romfill /32 images\basicc11.fc eproms\27256\basicfc.rom
if %floppy%==0 del eproms\27256\fdc344.rom 2>nul
if %scsi%==0   del eproms\27256\tmc850m.rom 2>nul
if %ide%==0    del eproms\27256\wdbios.rom 2>nul

if %floppy%==1 tools\romfill /64 images\fdc344.bin  eproms\27512\fdc344.rom
if %scsi%==1   tools\romfill /64 images\tmc850m.bin eproms\27512\tmc850m.rom
if %ide%==1    tools\romfill /64 images\wdbios.bin  eproms\27512\wdbios.rom
tools\romfill /64 xtbios.bin         eproms\27512\xtbios.rom
tools\romfill /64 images\basicc11.f6 eproms\27512\basicf6.rom
tools\romfill /64 images\basicc11.f8 eproms\27512\basicf8.rom
tools\romfill /64 images\basicc11.fa eproms\27512\basicfa.rom
tools\romfill /64 images\basicc11.fc eproms\27512\basicfc.rom
if %floppy%==0 del eproms\27512\fdc344.rom 2>nul
if %scsi%==0   del eproms\27512\tmc850m.rom 2>nul
if %ide%==0    del eproms\27512\wdbios.rom 2>nul

copy images\blank.bin images\new.bin
if %floppy%==1 tools\inject /0000 images\fdc344.bin  images\new.bin
if %scsi%==1   tools\inject /2000 images\tmc850m.bin images\new.bin
if %ide%==1    tools\inject /4000 images\wdbios.bin  images\new.bin
tools\inject /6000 images\basicc11.bin images\new.bin
tools\inject /E000 xtbios.bin   images\new.bin
tools\split images\new.bin eproms\split\bios.0 eproms\split\bios.1
del images\new.bin

@echo *******************************************************************************
@echo SUCCESS!: BIOS successfully built
@echo *******************************************************************************
goto end

:errasm
@echo.
@echo.
@echo *******************************************************************************
@echo ERROR: Error assembling BIOS
@echo *******************************************************************************
goto end

:errlink
@echo.
@echo.
@echo *******************************************************************************
@echo ERROR: Error linking BIOS
@echo *******************************************************************************
goto end

:end
set floppy=
set scsi=
set ide=
pause
