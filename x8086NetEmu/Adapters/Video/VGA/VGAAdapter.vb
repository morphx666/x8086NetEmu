﻿' http://www.osdever.net/FreeVGA/home.htm
' https://pdos.csail.mit.edu/6.828/2007/readings/hardware/vgadoc/VGABIOS.TXT
' http://stanislavs.org/helppc/ports.html

Public MustInherit Class VGAAdapter
    Inherits CGAAdapter

    Private ReadOnly VGABasePalette() As Color = {
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 169),
        Color.FromArgb(0, 169, 0),
        Color.FromArgb(0, 169, 169),
        Color.FromArgb(169, 0, 0),
        Color.FromArgb(169, 0, 169),
        Color.FromArgb(169, 169, 0),
        Color.FromArgb(169, 169, 169),
        Color.FromArgb(0, 0, 84),
        Color.FromArgb(0, 0, 255),
        Color.FromArgb(0, 169, 84),
        Color.FromArgb(0, 169, 255),
        Color.FromArgb(169, 0, 84),
        Color.FromArgb(169, 0, 255),
        Color.FromArgb(169, 169, 84),
        Color.FromArgb(169, 169, 255),
        Color.FromArgb(0, 84, 0),
        Color.FromArgb(0, 84, 169),
        Color.FromArgb(0, 255, 0),
        Color.FromArgb(0, 255, 169),
        Color.FromArgb(169, 84, 0),
        Color.FromArgb(169, 84, 169),
        Color.FromArgb(169, 255, 0),
        Color.FromArgb(169, 255, 169),
        Color.FromArgb(0, 84, 84),
        Color.FromArgb(0, 84, 255),
        Color.FromArgb(0, 255, 84),
        Color.FromArgb(0, 255, 255),
        Color.FromArgb(169, 84, 84),
        Color.FromArgb(169, 84, 255),
        Color.FromArgb(169, 255, 84),
        Color.FromArgb(169, 255, 255),
        Color.FromArgb(84, 0, 0),
        Color.FromArgb(84, 0, 169),
        Color.FromArgb(84, 169, 0),
        Color.FromArgb(84, 169, 169),
        Color.FromArgb(255, 0, 0),
        Color.FromArgb(255, 0, 169),
        Color.FromArgb(255, 169, 0),
        Color.FromArgb(255, 169, 169),
        Color.FromArgb(84, 0, 84),
        Color.FromArgb(84, 0, 255),
        Color.FromArgb(84, 169, 84),
        Color.FromArgb(84, 169, 255),
        Color.FromArgb(255, 0, 84),
        Color.FromArgb(255, 0, 255),
        Color.FromArgb(255, 169, 84),
        Color.FromArgb(255, 169, 255),
        Color.FromArgb(84, 84, 0),
        Color.FromArgb(84, 84, 169),
        Color.FromArgb(84, 255, 0),
        Color.FromArgb(84, 255, 169),
        Color.FromArgb(255, 84, 0),
        Color.FromArgb(255, 84, 169),
        Color.FromArgb(255, 255, 0),
        Color.FromArgb(255, 255, 169),
        Color.FromArgb(84, 84, 84),
        Color.FromArgb(84, 84, 255),
        Color.FromArgb(84, 255, 84),
        Color.FromArgb(84, 255, 255),
        Color.FromArgb(255, 84, 84),
        Color.FromArgb(255, 84, 255),
        Color.FromArgb(255, 255, 84),
        Color.FromArgb(255, 255, 255),
        Color.FromArgb(255, 125, 125),
        Color.FromArgb(255, 157, 125),
        Color.FromArgb(255, 190, 125),
        Color.FromArgb(255, 222, 125),
        Color.FromArgb(255, 255, 125),
        Color.FromArgb(222, 255, 125),
        Color.FromArgb(190, 255, 125),
        Color.FromArgb(157, 255, 125),
        Color.FromArgb(125, 255, 125),
        Color.FromArgb(125, 255, 157),
        Color.FromArgb(125, 255, 190),
        Color.FromArgb(125, 255, 222),
        Color.FromArgb(125, 255, 255),
        Color.FromArgb(125, 222, 255),
        Color.FromArgb(125, 190, 255),
        Color.FromArgb(125, 157, 255),
        Color.FromArgb(182, 182, 255),
        Color.FromArgb(198, 182, 255),
        Color.FromArgb(218, 182, 255),
        Color.FromArgb(234, 182, 255),
        Color.FromArgb(255, 182, 255),
        Color.FromArgb(255, 182, 234),
        Color.FromArgb(255, 182, 218),
        Color.FromArgb(255, 182, 198),
        Color.FromArgb(255, 182, 182),
        Color.FromArgb(255, 198, 182),
        Color.FromArgb(255, 218, 182),
        Color.FromArgb(255, 234, 182),
        Color.FromArgb(255, 255, 182),
        Color.FromArgb(234, 255, 182),
        Color.FromArgb(218, 255, 182),
        Color.FromArgb(198, 255, 182),
        Color.FromArgb(182, 255, 182),
        Color.FromArgb(182, 255, 198),
        Color.FromArgb(182, 255, 218),
        Color.FromArgb(182, 255, 234),
        Color.FromArgb(182, 255, 255),
        Color.FromArgb(182, 234, 255),
        Color.FromArgb(182, 218, 255),
        Color.FromArgb(182, 198, 255),
        Color.FromArgb(0, 0, 113),
        Color.FromArgb(28, 0, 113),
        Color.FromArgb(56, 0, 113),
        Color.FromArgb(84, 0, 113),
        Color.FromArgb(113, 0, 113),
        Color.FromArgb(113, 0, 84),
        Color.FromArgb(113, 0, 56),
        Color.FromArgb(113, 0, 28),
        Color.FromArgb(113, 0, 0),
        Color.FromArgb(113, 28, 0),
        Color.FromArgb(113, 56, 0),
        Color.FromArgb(113, 84, 0),
        Color.FromArgb(113, 113, 0),
        Color.FromArgb(84, 113, 0),
        Color.FromArgb(56, 113, 0),
        Color.FromArgb(28, 113, 0),
        Color.FromArgb(0, 113, 0),
        Color.FromArgb(0, 113, 28),
        Color.FromArgb(0, 113, 56),
        Color.FromArgb(0, 113, 84),
        Color.FromArgb(0, 113, 113),
        Color.FromArgb(0, 84, 113),
        Color.FromArgb(0, 56, 113),
        Color.FromArgb(0, 28, 113),
        Color.FromArgb(56, 56, 113),
        Color.FromArgb(68, 56, 113),
        Color.FromArgb(84, 56, 113),
        Color.FromArgb(97, 56, 113),
        Color.FromArgb(113, 56, 113),
        Color.FromArgb(113, 56, 97),
        Color.FromArgb(113, 56, 84),
        Color.FromArgb(113, 56, 68),
        Color.FromArgb(113, 56, 56),
        Color.FromArgb(113, 68, 56),
        Color.FromArgb(113, 84, 56),
        Color.FromArgb(113, 97, 56),
        Color.FromArgb(113, 113, 56),
        Color.FromArgb(97, 113, 56),
        Color.FromArgb(84, 113, 56),
        Color.FromArgb(68, 113, 56),
        Color.FromArgb(56, 113, 56),
        Color.FromArgb(56, 113, 68),
        Color.FromArgb(56, 113, 84),
        Color.FromArgb(56, 113, 97),
        Color.FromArgb(56, 113, 113),
        Color.FromArgb(56, 97, 113),
        Color.FromArgb(56, 84, 113),
        Color.FromArgb(56, 68, 113),
        Color.FromArgb(80, 80, 113),
        Color.FromArgb(89, 80, 113),
        Color.FromArgb(97, 80, 113),
        Color.FromArgb(105, 80, 113),
        Color.FromArgb(113, 80, 113),
        Color.FromArgb(113, 80, 105),
        Color.FromArgb(113, 80, 97),
        Color.FromArgb(113, 80, 89),
        Color.FromArgb(113, 80, 80),
        Color.FromArgb(113, 89, 80),
        Color.FromArgb(113, 97, 80),
        Color.FromArgb(113, 105, 80),
        Color.FromArgb(113, 113, 80),
        Color.FromArgb(105, 113, 80),
        Color.FromArgb(97, 113, 80),
        Color.FromArgb(89, 113, 80),
        Color.FromArgb(80, 113, 80),
        Color.FromArgb(80, 113, 89),
        Color.FromArgb(80, 113, 97),
        Color.FromArgb(80, 113, 105),
        Color.FromArgb(80, 113, 113),
        Color.FromArgb(80, 105, 113),
        Color.FromArgb(80, 97, 113),
        Color.FromArgb(80, 89, 113),
        Color.FromArgb(0, 0, 64),
        Color.FromArgb(16, 0, 64),
        Color.FromArgb(32, 0, 64),
        Color.FromArgb(48, 0, 64),
        Color.FromArgb(64, 0, 64),
        Color.FromArgb(64, 0, 48),
        Color.FromArgb(64, 0, 32),
        Color.FromArgb(64, 0, 16),
        Color.FromArgb(64, 0, 0),
        Color.FromArgb(64, 16, 0),
        Color.FromArgb(64, 32, 0),
        Color.FromArgb(64, 48, 0),
        Color.FromArgb(64, 64, 0),
        Color.FromArgb(48, 64, 0),
        Color.FromArgb(32, 64, 0),
        Color.FromArgb(16, 64, 0),
        Color.FromArgb(0, 64, 0),
        Color.FromArgb(0, 64, 16),
        Color.FromArgb(0, 64, 32),
        Color.FromArgb(0, 64, 48),
        Color.FromArgb(0, 64, 64),
        Color.FromArgb(0, 48, 64),
        Color.FromArgb(0, 32, 64),
        Color.FromArgb(0, 16, 64),
        Color.FromArgb(32, 32, 64),
        Color.FromArgb(40, 32, 64),
        Color.FromArgb(48, 32, 64),
        Color.FromArgb(56, 32, 64),
        Color.FromArgb(64, 32, 64),
        Color.FromArgb(64, 32, 56),
        Color.FromArgb(64, 32, 48),
        Color.FromArgb(64, 32, 40),
        Color.FromArgb(64, 32, 32),
        Color.FromArgb(64, 40, 32),
        Color.FromArgb(64, 48, 32),
        Color.FromArgb(64, 56, 32),
        Color.FromArgb(64, 64, 32),
        Color.FromArgb(56, 64, 32),
        Color.FromArgb(48, 64, 32),
        Color.FromArgb(40, 64, 32),
        Color.FromArgb(32, 64, 32),
        Color.FromArgb(32, 64, 40),
        Color.FromArgb(32, 64, 48),
        Color.FromArgb(32, 64, 56),
        Color.FromArgb(32, 64, 64),
        Color.FromArgb(32, 56, 64),
        Color.FromArgb(32, 48, 64),
        Color.FromArgb(32, 40, 64),
        Color.FromArgb(44, 44, 64),
        Color.FromArgb(48, 44, 64),
        Color.FromArgb(52, 44, 64),
        Color.FromArgb(60, 44, 64),
        Color.FromArgb(64, 44, 64),
        Color.FromArgb(64, 44, 60),
        Color.FromArgb(64, 44, 52),
        Color.FromArgb(64, 44, 48),
        Color.FromArgb(64, 44, 44),
        Color.FromArgb(64, 48, 44),
        Color.FromArgb(64, 52, 44),
        Color.FromArgb(64, 60, 44),
        Color.FromArgb(64, 64, 44),
        Color.FromArgb(60, 64, 44),
        Color.FromArgb(52, 64, 44),
        Color.FromArgb(48, 64, 44),
        Color.FromArgb(44, 64, 44),
        Color.FromArgb(44, 64, 48),
        Color.FromArgb(44, 64, 52),
        Color.FromArgb(44, 64, 60),
        Color.FromArgb(44, 64, 64),
        Color.FromArgb(44, 60, 64),
        Color.FromArgb(44, 52, 64),
        Color.FromArgb(44, 48, 64),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0)
    }

    Protected VGA_SC(&HFF - 1) As Byte
    Protected VGA_ATTR(&HFF - 1) As Byte
    Protected VGA_CRTC(&HFF - 1) As Byte
    Private ReadOnly VGA_GC(&HFF - 1) As Byte
    Private ReadOnly VGA_Latch(4 - 1) As Byte

    Private flip3C0 As Boolean = False
    Private stateDAC As Byte
    Private latchReadRGB As Integer
    Private latchReadPal As Integer
    Private latchWriteRGB As Integer
    Private latchWritePal As Integer

    Protected portRAM(&HFFF - 1) As Byte
    Protected vgaPalette(VGABasePalette.Length - 1) As Color
    Private mUseVRAM As Boolean

    Protected Const MEMSIZE As UInt32 = &H100000UI
    Protected vRAM(MEMSIZE - 1) As Byte

    Private Const planeSize As UInt32 = &H10000
    Private tmpRGB As UInt32
    Private tmpVal As Byte
    Private ramOffset As UInt32

    Private Const useROM As Boolean = True

    Private mCPU As X8086

    ' Video Modes: http://www.columbia.edu/~em36/wpdos/videomodes.txt
    '              http://webpages.charter.net/danrollins/techhelp/0114.HTM
    ' Ports: http://stanislavs.org/helppc/ports.html

    Public Sub New(cpu As X8086, Optional useInternalTimer As Boolean = True, Optional enableWebUI As Boolean = False)
        MyBase.New(cpu, useInternalTimer, enableWebUI)
        mCPU = cpu

        MyBase.vidModeChangeFlag = 0 ' Prevent the CGA adapter from changing video modes

        If useROM Then mCPU.LoadBIN("roms\ET4000(1-10-92).BIN", &HC000, &H0)
        'If useROM Then mCPU.LoadBIN("..\Other Emulators & Resources\fake86-0.12.9.19-win32\Binaries\videorom.bin", &HC000, &H0)
        'If useROM Then mCPU.LoadBIN("..\Other Emulators & Resources\PCemV0.7\roms\TRIDENT.BIN", &HC000, &H0)
        'If useROM Then mCPU.LoadBIN("roms\ET4000(4-7-93).BIN", &HC000, &H0)

        ValidPortAddress.Clear()
        For i As UInt32 = &H3C0 To &H3CF ' EGA/VGA
            ValidPortAddress.Add(i)
        Next
        ValidPortAddress.Add(&H3DA)

        For i As Integer = 0 To VGABasePalette.Length - 1
            vgaPalette(i) = VGABasePalette(i)
        Next

        mCPU.TryAttachHook(New X8086.MemHandler(Function(address As UInt32, ByRef value As UInt16, mode As X8086.MemHookMode) As Boolean
                                                    Select Case mMainMode
                                                        Case MainModes.Text
                                                            If address >= mStartTextVideoAddress AndAlso address < mEndTextVideoAddress Then
                                                                Select Case mode
                                                                    Case X8086.MemHookMode.Read
                                                                        value = VideoRAM(address - mStartTextVideoAddress)
                                                                    Case X8086.MemHookMode.Write
                                                                        VideoRAM(address - mStartTextVideoAddress) = value
                                                                End Select
                                                                Return True
                                                            End If
                                                            Return False
                                                        Case MainModes.Graphics
                                                            If address >= mStartGraphicsVideoAddress AndAlso address < mEndGraphicsVideoAddress Then
                                                                Select Case mode
                                                                    Case X8086.MemHookMode.Read
                                                                        value = VideoRAM(address - mStartGraphicsVideoAddress)
                                                                    Case X8086.MemHookMode.Write
                                                                        VideoRAM(address - mStartGraphicsVideoAddress) = value
                                                                End Select
                                                                Return True
                                                            End If
                                                            Return False
                                                    End Select
                                                    Return False
                                                End Function))

        mCPU.TryAttachHook(&H10, New X8086.IntHandler(Function() As Boolean
                                                          Select Case mCPU.Registers.AH
                                                              Case &H0
                                                                  VideoMode = mCPU.Registers.AL
                                                                  VGA_SC(4) = 0
                                                                  Return useROM ' When using ROM, prevent the BIOS from handling this function
                                                              Case &H10
                                                                  Select Case mCPU.Registers.AL
                                                                      Case &H10 ' Set individual DAC register
                                                                          vgaPalette(mCPU.Registers.BX Mod 256) = Color.FromArgb(RGBToUInt(CUInt(mCPU.Registers.DH And &H3F) << 2,
                                                                                                                                           CUInt(mCPU.Registers.CH And &H3F) << 2,
                                                                                                                                           CUInt(mCPU.Registers.CL And &H3F) << 2))
                                                                      Case &H12 ' Set block of DAC registers
                                                                          Dim addr As Integer = CUInt(mCPU.Registers.ES) * 16UI + mCPU.Registers.DX
                                                                          For n As Integer = mCPU.Registers.BX To mCPU.Registers.BX + mCPU.Registers.CX - 1
                                                                              vgaPalette(n) = Color.FromArgb(RGBToUInt(mCPU.Memory(addr + 0) << 2,
                                                                                                                       mCPU.Memory(addr + 1) << 2,
                                                                                                                       mCPU.Memory(addr + 2) << 2))
                                                                              addr += 3
                                                                          Next
                                                                  End Select
                                                              Case &H1A
                                                                  mCPU.Registers.AL = &H1A ' http://stanislavs.org/helppc/int_10-1a.html
                                                                  mCPU.Registers.BL = &H8
                                                                  Return True
                                                          End Select

                                                          Return False
                                                      End Function))
    End Sub

    Public ReadOnly Property UseVRAM As Boolean
        Get
            Return mUseVRAM
        End Get
    End Property

    Public Property VideoRAM(address As UInt16) As Byte
        Get
            If Not mUseVRAM Then
                Return mCPU.Memory(address + ramOffset)
            ElseIf (VGA_SC(4) And 6) = 0 AndAlso mVideoMode <> &HD AndAlso mVideoMode <> &H10 AndAlso mVideoMode <> &H12 Then
                Return mCPU.Memory(address + ramOffset)
            Else
                Return Read(address)
            End If
        End Get
        Set(value As Byte)
            If Not mUseVRAM Then
                mCPU.Memory(address + ramOffset) = value
            ElseIf (VGA_SC(4) And 6) = 0 AndAlso mVideoMode <> &HD AndAlso mVideoMode <> &H10 AndAlso mVideoMode <> &H12 Then
                mCPU.Memory(address + ramOffset) = value
            Else
                Write(address, value)
            End If
        End Set
    End Property

    Public Overrides Property VideoMode As UInt32
        Get
            Return mVideoMode
        End Get
        Set(value As UInt32)
            Select Case value >> 8 ' Mode is in AH
                Case 0 ' Set video mode
                    value = value And &H7F ' http://stanislavs.org/helppc/ports.html
                    mVideoMode = value
                    X8086.Notify($"VGA Video Mode: {CShort(mVideoMode):X2}", X8086.NotificationReasons.Info)

                    Select Case mVideoMode
                        Case 0 ' 40x25 Mono Text
                            mStartTextVideoAddress = &HB8000
                            mStartGraphicsVideoAddress = &HB8000
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(360, 400)
                            mCellSize = New Size(9, 16)
                            mMainMode = MainModes.Text
                            mPixelsPerByte = 4
                            mUseVRAM = False

                        Case 1 ' 40x25 Color Text
                            mStartTextVideoAddress = &HB8000
                            mStartGraphicsVideoAddress = &HB8000
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(360, 400)
                            mCellSize = New Size(9, 16)
                            mMainMode = MainModes.Text
                            mPixelsPerByte = 4
                            mUseVRAM = False
                            portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                        Case 2 ' 80x25 Mono Text
                            mStartTextVideoAddress = &HB8000
                            mStartGraphicsVideoAddress = &HB8000
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(640, 400)
                            mCellSize = New Size(9, 16)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 4
                            mUseVRAM = False
                            portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                        Case 3 ' 80x25 Color Text
                            mStartTextVideoAddress = &HB8000
                            mStartGraphicsVideoAddress = &HB8000
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(720, 400)
                            mCellSize = New Size(9, 16)
                            mMainMode = MainModes.Text
                            mPixelsPerByte = 4
                            mUseVRAM = False
                            portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                        Case 4, 5 ' 320x200 4 Colors
                            mStartTextVideoAddress = &HB8000
                            mStartGraphicsVideoAddress = &HB8000
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(320, 200)
                            mCellSize = New Size(8, 8)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 4
                            mUseVRAM = False
                            'portRAM(&H3D9) = If(value And &HF = 4, 48, 0)
                            If mCPU.Registers.AL = 4 Then
                                portRAM(&H3D9) = 48
                            Else
                                portRAM(&H3D9) = 0
                            End If

                        Case 6 ' 640x200 2 Colors
                            mStartTextVideoAddress = &HB8000
                            mStartGraphicsVideoAddress = &HB8000
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(640, 200)
                            mCellSize = New Size(8, 8)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 2
                            mUseVRAM = False
                            portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                        Case 7 ' 640x200 2 Colors
                            mStartTextVideoAddress = &HB0000
                            mStartGraphicsVideoAddress = &HB0000
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(720, 400)
                            mCellSize = New Size(9, 16)
                            mMainMode = MainModes.Text
                            mPixelsPerByte = 1
                            mUseVRAM = False

                        Case 9 ' 320x200 16 Colors
                            mStartTextVideoAddress = &HB8000
                            mStartGraphicsVideoAddress = &HB8000
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(320, 200)
                            mCellSize = New Size(8, 8)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 4
                            mUseVRAM = False
                            portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                        Case &HD ' 320x200 16 Colors
                            mStartTextVideoAddress = &HA0000
                            mStartGraphicsVideoAddress = &HA0000
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(320, 200)
                            mCellSize = New Size(8, 8)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 4
                            mUseVRAM = True
                            portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                        Case &HE ' 640x200 16 Colors
                            mStartTextVideoAddress = &HA0000
                            mStartGraphicsVideoAddress = &HA0000
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(640, 200)
                            mCellSize = New Size(8, 8)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 4
                            mUseVRAM = True

                        Case &H10 ' 640x350 4 Colors
                            mStartTextVideoAddress = &HA0000
                            mStartGraphicsVideoAddress = &HA0000
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(640, 350)
                            mCellSize = New Size(8, 14)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 4
                            mUseVRAM = True

                        Case &H12
                            mStartTextVideoAddress = &HA0000
                            mStartGraphicsVideoAddress = &HA0000
                            mTextResolution = New Size(80, 30)
                            mVideoResolution = New Size(640, 480)
                            mCellSize = New Size(8, 16)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 4
                            mUseVRAM = True
                            portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                        Case &H13
                            mStartTextVideoAddress = &HA0000
                            mStartGraphicsVideoAddress = &HA0000
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(320, 200)
                            mCellSize = New Size(8, 8)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 4
                            mUseVRAM = True
                            portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                        Case 127 ' 90x25 Mono Text
                            mStartTextVideoAddress = &HB0000
                            mStartGraphicsVideoAddress = &HB0000
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(720, 400)
                            mCellSize = New Size(8, 16)
                            mMainMode = MainModes.Text
                            mPixelsPerByte = 1
                            portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                            'Case &H30 ' 800x600 Color Tseng ET3000/4000 chipset
                            '    mStartTextVideoAddress = &HA0000
                            '    mStartGraphicsVideoAddress = &HA0000
                            '    mTextResolution = New Size(100, 37)
                            '    mVideoResolution = New Size(800, 600)
                            '    mCellSize = New Size(8, 8)
                            '    mMainMode = MainModes.Graphics
                            '    mPixelsPerByte = 4
                            '    portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                        Case Else
                            'mStartTextVideoAddress = &HB0000
                            'mStartGraphicsVideoAddress = &HB0000
                            'mTextResolution = New Size(132, 25)
                            'mVideoResolution = New Size(640, 200)
                            'mCellSize = New Size(8, 8)
                            'mMainMode = MainModes.Text
                            'mPixelsPerByte = 1
                            'mUseVRAM = False

                    End Select

                    mCellSize = New Size(8, 16) ' Temporary hack until we can stretch the fonts' bitmaps
                    mCPU.Memory(&H449) = mVideoMode
                    mCPU.Memory(&H44A) = mTextResolution.Width
                    mCPU.Memory(&H44B) = 0
                    mCPU.Memory(&H484) = mTextResolution.Height - 1
                    'mCPU.Memory(&H463) = &H3D4 ' With and without a BIOS INT 10,8/9/10 fails to work

                    InitVideoMemory(False)

            End Select
        End Set
    End Property

    Private Function RGBToUInt(r As UInt16, g As UInt16, b As UInt16) As UInt16
        Return r Or (g << 8) Or (b << 16)
    End Function

    Public Overrides Function [In](port As UInt32) As UInt16
        Select Case port
            Case &H3C1 : Return VGA_ATTR(portRAM(&H3C0))

            Case &H3C5 : Return VGA_SC(portRAM(&H3C4))

            Case &H3D5 : Return VGA_CRTC(portRAM(&H3D4))

            Case &H3C7 : Return stateDAC

            Case &H3C8 : Return latchReadPal

            Case &H3C9
                Select Case latchReadRGB
                    Case 0 ' B
                        tmpRGB = vgaPalette(latchReadPal).ToArgb() >> 2
                    Case 1 ' G
                        tmpRGB = vgaPalette(latchReadPal).ToArgb() >> 10
                    Case 2 ' R
                        tmpRGB = vgaPalette(latchReadPal).ToArgb() >> 18
                        latchReadPal += 1
                        latchReadRGB = -1
                End Select
                latchReadRGB += 1
                Return tmpRGB And &H3F

            Case &H3DA
                flip3C0 = True ' https://wiki.osdev.org/VGA_Hardware#Port_0x3C0
                Return MyBase.In(port)

        End Select

        Return portRAM(port)
    End Function

    Public Overrides Sub Out(port As UInt32, value As UInt16)
        Select Case port
            Case &H3C0 ' https://wiki.osdev.org/VGA_Hardware#Port_0x3C0
                If flip3C0 Then
                    portRAM(port) = value
                Else
                    VGA_ATTR(portRAM(port)) = value
                End If
                flip3C0 = Not flip3C0

            Case &H3C4 ' Sequence controller index
                portRAM(port) = value

            Case &H3C5 ' Sequence controller data
                VGA_SC(portRAM(&H3C4)) = value

            Case &H3C7 ' Color index register (read operations)
                latchReadPal = value
                latchReadRGB = 0
                stateDAC = 0

            Case &H3C8 ' Color index register (write operations)
                latchWritePal = value
                latchWriteRGB = 0
                tmpRGB = 0
                stateDAC = 3

            Case &H3C9 ' RGB data register
                value = value And &H3F
                Select Case latchWriteRGB
                    Case 0 ' R
                        tmpRGB = CUInt(value) << 2
                    Case 1 ' G
                        tmpRGB = tmpRGB Or (CUInt(value) << 10)
                    Case 2 ' B
                        vgaPalette(latchWritePal) = Color.FromArgb(tmpRGB Or (CUInt(value) << 18))
                        'vgaPalette(latchWritePal) = Color.FromArgb((&HFF << 24) Or tmpRGB Or (CUInt(value) << 18))
                        latchWritePal += 1
                End Select
                latchWriteRGB = (latchWriteRGB + 1) Mod 3

            Case &H3D4 ' 6845 index register
                portRAM(port) = value
                MyBase.Out(port, value)

            Case &H3D5 ' 6845 data register
                VGA_CRTC(portRAM(&H3D4)) = value
                MyBase.Out(port, value)

            'Case &H3CE ' VGA graphics index
            '    portRAM(port) = value Mod &H8 ' FIXME: This is one fugly hack!

            Case &H3CF
                VGA_GC(portRAM(&H3CE)) = value

                'Case &H3B8
                '    portRAM(port) = value
                '    MyBase.Out(port, value)

            Case Else
                portRAM(port) = value

        End Select
    End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return "VGA"
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "VGA Video Adapter"
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMajor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMinor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionRevision As Integer
        Get
            Return 1
        End Get
    End Property

    Public Overrides Sub Reset()
        MyBase.Reset()
        InitVideoMemory(False)
    End Sub

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
        If Not isInit Then Exit Sub

        MyBase.InitVideoMemory(clearScreen)

        mEndGraphicsVideoAddress = mStartGraphicsVideoAddress + 128 * 1024 ' 128KB
        ramOffset = If(mMainMode = MainModes.Text, mStartTextVideoAddress, mStartGraphicsVideoAddress)

        AutoSize()
    End Sub

    Public Overrides Sub Write(address As UInt32, value As UInt16)
        Dim curValue As Byte

        Select Case VGA_GC(5) And 3
            Case 0
                value = ShiftVGA(value)

                If (VGA_SC(2) And 1) <> 0 Then
                    If (VGA_GC(1) And 1) <> 0 Then
                        curValue = If((VGA_GC(0) And 1) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(0))
                    vRAM(address + planeSize * 0) = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(0))
                End If

                If (VGA_SC(2) And 2) <> 0 Then
                    If (VGA_GC(1) And 2) <> 0 Then
                        curValue = If((VGA_GC(0) And 2) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(1))
                    vRAM(address + planeSize * 1) = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(1))
                End If

                If (VGA_SC(2) And 4) <> 0 Then
                    If (VGA_GC(1) And 4) <> 0 Then
                        curValue = If((VGA_GC(0) And 4) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(2))
                    vRAM(address + planeSize * 2) = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(2))
                End If

                If (VGA_SC(2) And 8) <> 0 Then
                    If (VGA_GC(1) And 8) <> 0 Then
                        curValue = If((VGA_GC(0) And 8) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(3))
                    vRAM(address + planeSize * 3) = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(3))
                End If

            Case 1
                If (VGA_SC(2) And 1) <> 0 Then vRAM(address + planeSize * 0) = VGA_Latch(0)
                If (VGA_SC(2) And 2) <> 0 Then vRAM(address + planeSize * 1) = VGA_Latch(1)
                If (VGA_SC(2) And 4) <> 0 Then vRAM(address + planeSize * 2) = VGA_Latch(2)
                If (VGA_SC(2) And 8) <> 0 Then vRAM(address + planeSize * 3) = VGA_Latch(3)

            Case 2
                If (VGA_SC(2) And 1) <> 0 Then
                    If (VGA_GC(1) And 1) <> 0 Then
                        If (value And 1) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    Else
                        curValue = value
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(0))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(0))
                    vRAM(address + planeSize * 0) = curValue
                End If

                If (VGA_SC(2) And 2) <> 0 Then
                    If (VGA_GC(1) And 2) <> 0 Then
                        If (value And 2) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    Else
                        curValue = value
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(1))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(1))
                    vRAM(address + planeSize * 1) = curValue
                End If

                If (VGA_SC(2) And 4) <> 0 Then
                    If (VGA_GC(1) And 4) <> 0 Then
                        If (value And 4) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    Else
                        curValue = value
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(2))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(2))
                    vRAM(address + planeSize * 2) = curValue
                End If

                If (VGA_SC(2) And 8) <> 0 Then
                    If (VGA_GC(1) And 8) <> 0 Then
                        If (value And 8) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    Else
                        curValue = value
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(3))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(3))
                    vRAM(address + planeSize * 3) = curValue
                End If

            Case 3
                tmpVal = value And VGA_GC(8)
                value = ShiftVGA(value)

                If (VGA_SC(2) And 1) <> 0 Then
                    If (VGA_GC(0) And 1) <> 0 Then
                        If (value And 1) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(0))
                    curValue = ((Not tmpVal) And curValue) Or (tmpVal And VGA_Latch(0))
                    vRAM(address + planeSize * 0) = curValue
                End If

                If (VGA_SC(2) And 2) <> 0 Then
                    If (VGA_GC(0) And 2) <> 0 Then
                        If (value And 2) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(1))
                    curValue = ((Not tmpVal) And curValue) Or (tmpVal And VGA_Latch(1))
                    vRAM(address + planeSize * 1) = curValue
                End If

                If (VGA_SC(2) And 4) <> 0 Then
                    If (VGA_GC(0) And 4) <> 0 Then
                        If (value And 4) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(2))
                    curValue = ((Not tmpVal) And curValue) Or (tmpVal And VGA_Latch(2))
                    vRAM(address + planeSize * 2) = curValue
                End If

                If (VGA_SC(2) And 8) <> 0 Then
                    If (VGA_GC(0) And 8) <> 0 Then
                        If (value And 8) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_Latch(3))
                    curValue = ((Not tmpVal) And curValue) Or (tmpVal And VGA_Latch(3))
                    vRAM(address + planeSize * 3) = curValue
                End If
        End Select
    End Sub

    Public Overrides Function Read(address As UInt32) As UInt16
        VGA_Latch(0) = vRAM(address + planeSize * 0)
        VGA_Latch(1) = vRAM(address + planeSize * 1)
        VGA_Latch(2) = vRAM(address + planeSize * 2)
        VGA_Latch(3) = vRAM(address + planeSize * 3)

        If (VGA_SC(2) And 1) <> 0 Then Return vRAM(address + planeSize * 0)
        If (VGA_SC(2) And 2) <> 0 Then Return vRAM(address + planeSize * 1)
        If (VGA_SC(2) And 4) <> 0 Then Return vRAM(address + planeSize * 2)
        If (VGA_SC(2) And 8) <> 0 Then Return vRAM(address + planeSize * 3)

        Return 0
    End Function

    Private Function ShiftVGA(value As Byte) As Byte
        For i As Integer = 0 To (VGA_GC(3) And 7) - 1
            value = (value >> 1) Or ((value And 1) << 7)
        Next
        Return value
    End Function

    Private Function LogicVGA(curValue As Byte, latchValue As Byte) As Byte
        Select Case (VGA_GC(3) >> 3) And 3 ' Raster Op
            Case 1 : Return curValue And latchValue
            Case 2 : Return curValue Or latchValue
            Case 3 : Return curValue Xor latchValue
            Case Else : Return curValue
        End Select
    End Function
End Class
