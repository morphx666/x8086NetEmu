Public MustInherit Class VGAAdapter
    Inherits CGAAdapter

    Private VGABasePalette() As Color = {
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

    Private mVRAM(&H40000 - 1) As UInteger
    Public VGA_SC(&H100 - 1) As UInteger
    Protected VGA_CRTC(&H100 - 1) As UInteger
    Protected VGA_ATTR(&H100 - 1) As UInteger
    Protected VGA_GC(&H100 - 1) As UInteger

    Private flip3C0 As Boolean = False
    Private VGA_latch(4 - 1) As UInteger
    Private stateDAC As UInteger
    Private latchReadRGB As UInteger
    Private latchReadPal As UInteger
    Private latchWriteRGB As UInteger
    Private latchWritePal As UInteger
    Protected portRAM(&H10000 - 1) As UInteger
    Private tempRGB As UInteger
    Private mVGAPalette(VGABasePalette.Length - 1) As UInteger
    Private mUseVRAM As Boolean

    'Private port3DA As UInteger
    Private Const planeSize As UInteger = &H10000
    Private lastScanLineTick As Long
    Private scanLineTiming As Long = (Scheduler.CLOCKRATE / X8086.KHz) / 31500
    Private curScanLine As Long
    Private cursorPosition As UInteger

    Private useROM As Boolean = True ' FIXME: Enabling ROM support breaks CGA compatibility

    Protected lockObject As New Object()

    Private mCPU As X8086

    ' Video Modes: http://www.columbia.edu/~em36/wpdos/videomodes.txt
    ' Ports: http://stanislavs.org/helppc/ports.html

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)
        mCPU = cpu

        If useROM Then
            'mCPU.LoadBIN("roms\ET4000(1-10-92).BIN", &HC000, &H0)
            'mCPU.LoadBIN("..\..\Other Emulators & Resources\PCemV0.7\roms\TRIDENT.BIN", &HC000, &H0)
            mCPU.LoadBIN("roms\ET4000(4-7-93).BIN", &HC000, &H0)
        End If

        ValidPortAddress.Clear()
        ValidPortAddress.Add(&H3BA) ' Dummy port to speed ET4000 ROM initialization
        ValidPortAddress.Add(&H3B8) ' Monochrome support
        For i As UInteger = &H3C0 To &H3CF ' EGA/VGA
            ValidPortAddress.Add(i)
        Next
        For i As UInteger = &H3D0 To &H3DA ' CGA Adapter
            ValidPortAddress.Add(i)
        Next

        For i As Integer = 0 To VGABasePalette.Length - 1
            mVGAPalette(i) = VGABasePalette(i).ToArgb()
        Next

        mCPU.TryAttachHook(&H10, New X8086.IntHandler(Function()
                                                          If mCPU.Registers.AH = 0 OrElse mCPU.Registers.AH = &H10 Then
                                                              VideoMode = mCPU.Registers.AX
                                                              'If mCPU.Registers.AH = &H10 Then Return False
                                                              Return useROM
                                                          ElseIf mCPU.Registers.AH = &H1A And mCPU.Registers.AL = 0 Then ' Get display combination code (ps, vga/mcga)
                                                              mCPU.Registers.AL = &H1A ' http://stanislavs.org/helppc/int_10-1a.html
                                                              mCPU.Registers.BL = &H8
                                                              Return True
                                                          End If

                                                          Return False
                                                      End Function))

        mCPU.TryAttachHook(New X8086.MemHandler(Function(address As UInteger, ByRef value As UInteger, mode As X8086.MemHookMode)
                                                    If mUseVRAM AndAlso (address >= &HA0000 AndAlso address <= &HBFFFF) Then
                                                        If mVideoMode = &H13 AndAlso (VGA_SC(4) And 6) = 0 Then ' Mode 13h with plane mode
                                                            Return False
                                                        Else
                                                            If mode = X8086.MemHookMode.Read Then
                                                                value = Read(address - &HA0000)
                                                            Else
                                                                Write(address - &HA0000, value)
                                                            End If
                                                            Return True
                                                        End If
                                                    Else
                                                        Return False
                                                    End If
                                                End Function))

        'mCPU.TryAttachHook(8, New X8086.IntHandler(Function()
        '                                               Dim t As Long = mCPU.Sched.CurrentTime
        '                                               If t >= (lastScanLineTick + scanLineTiming) Then
        '                                                   curScanLine = (curScanLine + 1) Mod 525
        '                                                   If curScanLine > 479 Then
        '                                                       port3DA = 8
        '                                                   ElseIf (curScanLine And 1) <> 0 Then
        '                                                       port3DA = port3DA Or 1
        '                                                   End If
        '                                                   lastScanLineTick = t
        '                                               End If

        '                                               Return False
        '                                           End Function))

        lastScanLineTick = 0
    End Sub

    Public ReadOnly Property UseVRAM As Boolean
        Get
            Return mUseVRAM
        End Get
    End Property

    Public ReadOnly Property VGAPalette(index As Integer) As UInteger
        Get
            Return mVGAPalette(index)
        End Get
    End Property

    Public ReadOnly Property VRAM(address As UInteger) As UInteger
        Get
#If DEBUG Then
            If useROM Then
                Return mVRAM(address)
            Else
                Return mCPU.Memory(address)
            End If
#Else
            Return mVRAM(address)
#End If
        End Get
    End Property

    Public Overrides Property VideoMode As UInteger
        Get
            Return mVideoMode
        End Get
        Set(value As UInteger)
            Select Case value >> 8
                Case 0 ' Set video mode
                    value = value And &H7F
                    mVideoMode = value And &H7F ' http://stanislavs.org/helppc/ports.html
                    Debug.WriteLine($"VGA Video Mode: {CShort(mVideoMode):X2}")

                    VGA_SC(&H4) = 0
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

                        Case 2 ' 80x25 Mono Text
                            mStartTextVideoAddress = &HB8000
                            mStartGraphicsVideoAddress = &HB8000
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(720, 400)
                            mCellSize = New Size(9, 16)
                            mMainMode = MainModes.Text
                            mPixelsPerByte = 4
                            mUseVRAM = False

                        Case 3 ' 80x25 Color Text
                            mStartTextVideoAddress = &HB8000
                            mStartGraphicsVideoAddress = &HB8000
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(720, 400)
                            mCellSize = New Size(9, 16)
                            mMainMode = MainModes.Text
                            mPixelsPerByte = 4
                            mUseVRAM = False

                        Case 4, 5 ' 320x200 4 Colors
                            mStartTextVideoAddress = &HB8000
                            mStartGraphicsVideoAddress = &HB8000
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(320, 200)
                            mCellSize = New Size(8, 8)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 4
                            mUseVRAM = False
                            If value And &HF = 4 Then
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

                        Case &HD ' 320x200 16 Colors
                            mStartTextVideoAddress = &HA0000
                            mStartGraphicsVideoAddress = &HA0000
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(320, 200)
                            mCellSize = New Size(8, 8)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 4
                            mUseVRAM = True

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

                        Case &H13
                            mStartTextVideoAddress = &HA0000
                            mStartGraphicsVideoAddress = &HA0000
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(320, 200)
                            mCellSize = New Size(8, 8)
                            mMainMode = MainModes.Graphics
                            mPixelsPerByte = 4
                            mUseVRAM = True

                        Case 127 ' 90x25 Mono Text
                            mStartTextVideoAddress = &HB0000
                            mStartGraphicsVideoAddress = &HB0000
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(720, 400)
                            mCellSize = New Size(8, 16)
                            mMainMode = MainModes.Text
                            mPixelsPerByte = 1

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
                            mStartTextVideoAddress = &HB0000
                            mStartGraphicsVideoAddress = &HB0000
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(640, 200)
                            mCellSize = New Size(8, 8)
                            mMainMode = MainModes.Text
                            mPixelsPerByte = 1
                            mUseVRAM = False

                    End Select

                    mCellSize = New Size(8, 16) ' Temporary hack until we can stretch the fonts' bitmaps
                    portRAM(&H3D8) = portRAM(&H3D8) And &HFE
                    mCPU.RAM(&H449) = mVideoMode
                    mCPU.RAM(&H44A) = mTextResolution.Width
                    mCPU.RAM(&H44B) = 0
                    mCPU.RAM(&H484) = mTextResolution.Height - 1

                    mCursorCol = 0
                    mCursorRow = 0

                    If (value And &H80) = 0 Then
                        Array.Clear(mCPU.Memory, &HA0000, &H1FFFF)
                        Array.Clear(mVRAM, 0, mVRAM.Length)
                    End If

                    InitVideoMemory(False)

                Case &H10 ' VGA DAC functions
                    Select Case value And &HFF
                        Case &H10 ' Set individual DAC register
                            mVGAPalette(mCPU.Registers.BX) = RGBToUint((mCPU.Registers.DH And 63) << 2,
                                                                      (mCPU.Registers.CH And 63) << 2,
                                                                      (mCPU.Registers.CL And 63) << 2)
                        Case &H12 ' Set block of DAC registers
                            Dim addr As Integer = mCPU.Registers.ES * 16 + mCPU.Registers.DX
                            For n As Integer = mCPU.Registers.BX To mCPU.Registers.BX + mCPU.Registers.CX - 1
                                mVGAPalette(n) = RGBToUint(mCPU.RAM(addr + 0) << 2,
                                                          mCPU.RAM(addr + 1) << 2,
                                                          mCPU.RAM(addr + 2) << 2)
                                addr += 3
                            Next

                    End Select

            End Select
        End Set
    End Property

    Private Function RGBToUint(r As UInteger, g As UInteger, b As UInteger) As UInteger
        Return r Or (g << 8) Or (b << 16)
    End Function

    Public Overrides Function [In](port As UInteger) As UInteger
        Select Case port
            Case &H3C1
                Return VGA_ATTR(portRAM(&H3C0)) And &H1F

            Case &H3C5
                Return VGA_SC(portRAM(&H3C4)) And &H1F

            Case &H3D5
                Return VGA_CRTC(portRAM(&H3D4)) And &H1F

            Case &H3C7
                Return stateDAC

            Case &H3C8
                Return latchReadPal

            Case &H3C9
                latchReadRGB += 1
                Select Case (latchReadRGB - 1)
                    Case 0 ' R
                        Return (mVGAPalette(latchReadPal - 1) >> 18) And &H3F
                    Case 1 ' G
                        Return (mVGAPalette(latchReadPal) >> 10) And &H3F
                    Case 2 ' B
                        latchReadRGB = 0
                        latchReadPal += 1
                        Return (mVGAPalette(latchReadPal) >> 2) And &H3F
                End Select

            Case &H3DA ' Using the CGA timing code appears to solve many problems
                flip3C0 = True ' https://wiki.osdev.org/VGA_Hardware#Port_0x3C0

                Dim t As Long = mCPU.Sched.CurrentTime
                Dim hRetrace As Boolean = (t Mod ht) <= (ht / 10)
                Dim vRetrace As Boolean = (t Mod vt) <= (vt / 10)

                Return If(hRetrace, 1, 0) Or If(vRetrace, 8, 0)
                'Return port3DA

                'Case >= &H3D0
                '    Return MyBase.In(port)

        End Select

        Return portRAM(port)
    End Function

    Public Overrides Sub Out(port As UInteger, value As UInteger)
        value = value And &HFF
        Select Case port
            Case &H3B8
                If ((value And 2) = 2) AndAlso VideoMode <> 127 Then
                    VideoMode = 127
                End If

            Case &H3C0 ' https://wiki.osdev.org/VGA_Hardware#Port_0x3C0
                If flip3C0 Then
                    portRAM(&H3C0) = value
                Else
                    VGA_ATTR(portRAM(&H3C0)) = value
                    ' This doesn't work when using ROM
                    'If portRAM(&H3C0) = &H10 Then mBlinkCharOn = (value And &B100) <> 0
                End If
                flip3C0 = Not flip3C0

            Case &H3C4 ' Sequence controller index
                portRAM(&H3C4) = value

            Case &H3C5 ' Sequence controller data
                VGA_SC(portRAM(&H3C4)) = value
                ' This doesn't work when using ROM
                'If portRAM(&H3C4) = &H1 Then mVideoEnabled = (value And &B100000) <> 0

            Case &H3C7 ' Color index register (read operations)
                latchReadPal = value
                latchReadRGB = 0
                stateDAC = 0

            Case &H3C8 ' Color index register (write operations)
                latchWritePal = value
                latchWriteRGB = 0
                tempRGB = 0
                stateDAC = 3

            Case &H3C9 ' RGB data register
                Select Case latchWriteRGB
                    Case 0 ' R
                        tempRGB = ((value And &H3F) << 18)
                    Case 1 ' G
                        tempRGB = tempRGB Or ((value And &H3F) << 10)
                    Case 2 ' B
                        mVGAPalette(latchWritePal) = tempRGB Or (value And &H3F) << 2
                        latchWritePal += 1
                End Select
                latchWriteRGB = (latchWriteRGB + 1) Mod 3

            Case &H3D4 ' 6845 index register
                portRAM(&H3D4) = value

            Case &H3D5 ' 6845 data register
                VGA_CRTC(portRAM(&H3D4)) = value

                ' This doesn't work when using ROM
                Select Case portRAM(&H3D4)
                    Case &HA
                        mCursorVisible = (value And &HB100000) <> 0
                        mCursorStart = value And &HB11111
                    Case &HB
                        mCursorEnd = value And &HB11111
                    Case &HE : cursorPosition = (cursorPosition And &HFF) Or (value << 8)
                    Case &HF : cursorPosition = (cursorPosition And &HFF00) Or value
                End Select
                cursorPosition = cursorPosition And &HFFFF
                mCursorRow = cursorPosition / mTextResolution.Width
                mCursorCol = cursorPosition Mod mTextResolution.Width

            Case &H3CE
                VGA_GC(portRAM(&H3CE)) = value 'And &HF

            Case &H3CF
                VGA_GC(portRAM(&H3CE)) = value

                'Case >= &H3D0
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

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
        If Not isInit Then Exit Sub

        'Dim ppb As Integer = mPixelsPerByte
        MyBase.InitVideoMemory(clearScreen)
        'mPixelsPerByte = ppb

        AutoSize()
    End Sub

    Public Overrides Sub Write(address As UInteger, value As UInteger)
        Dim curValue As UInteger = ShiftVGA(value)

        Select Case (VGA_GC(5) And 3)
            Case 0
                If (VGA_SC(2) And 1) <> 0 Then
                    If (VGA_GC(1) And 1) <> 0 Then
                        If (VGA_GC(0) And 1) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(0))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_latch(0))
                    mVRAM(address + planeSize * 0) = curValue
                End If

                If (VGA_SC(2) And 2) <> 0 Then
                    If (VGA_GC(1) And 2) <> 0 Then
                        If (VGA_GC(0) And 2) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(1))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_latch(1))
                    mVRAM(address + planeSize * 1) = curValue
                End If

                If (VGA_SC(2) And 4) <> 0 Then
                    If (VGA_GC(1) And 4) <> 0 Then
                        If (VGA_GC(0) And 4) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(2))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_latch(2))
                    mVRAM(address + planeSize * 2) = curValue
                End If

                If (VGA_SC(2) And 8) <> 0 Then
                    If (VGA_GC(1) And 8) <> 0 Then
                        If (VGA_GC(0) And 8) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(3))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_latch(3))
                    mVRAM(address + planeSize * 3) = curValue
                End If

            Case 1
                If (VGA_SC(2) And 1) <> 0 Then mVRAM(address + planeSize * 0) = VGA_latch(0)
                If (VGA_SC(2) And 2) <> 0 Then mVRAM(address + planeSize * 1) = VGA_latch(1)
                If (VGA_SC(2) And 4) <> 0 Then mVRAM(address + planeSize * 2) = VGA_latch(2)
                If (VGA_SC(2) And 8) <> 0 Then mVRAM(address + planeSize * 3) = VGA_latch(3)

            Case 2
                If (VGA_SC(2) And 1) <> 0 Then
                    If (VGA_GC(1) And 1) <> 0 Then
                        If (value And 1) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(0))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_latch(0))
                    mVRAM(address + planeSize * 0) = curValue
                End If

                If (VGA_SC(2) And 2) <> 0 Then
                    If (VGA_GC(1) And 2) <> 0 Then
                        If (value And 2) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(1))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_latch(1))
                    mVRAM(address + planeSize * 1) = curValue
                End If

                If (VGA_SC(2) And 4) <> 0 Then
                    If (VGA_GC(1) And 4) <> 0 Then
                        If (value And 4) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(2))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_latch(2))
                    mVRAM(address + planeSize * 2) = curValue
                End If

                If (VGA_SC(2) And 8) <> 0 Then
                    If (VGA_GC(1) And 8) <> 0 Then
                        If (value And 8) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(3))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_latch(3))
                    mVRAM(address + planeSize * 3) = curValue
                End If

            Case 3
                Dim tmp As UInteger = value And VGA_GC(8)

                If (VGA_SC(2) And 1) <> 0 Then
                    If (VGA_GC(0) And 1) <> 0 Then
                        If (value And 1) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(0))
                    curValue = ((Not tmp) And curValue) Or (tmp And VGA_latch(0))
                    mVRAM(address + planeSize * 0) = curValue
                End If

                If (VGA_SC(2) And 2) <> 0 Then
                    If (VGA_GC(0) And 2) <> 0 Then
                        If (value And 2) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(1))
                    curValue = ((Not tmp) And curValue) Or (tmp And VGA_latch(1))
                    mVRAM(address + planeSize * 1) = curValue
                End If

                If (VGA_SC(2) And 4) <> 0 Then
                    If (VGA_GC(0) And 4) <> 0 Then
                        If (value And 4) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(2))
                    curValue = ((Not tmp) And curValue) Or (tmp And VGA_latch(2))
                    mVRAM(address + planeSize * 2) = curValue
                End If

                If (VGA_SC(2) And 8) <> 0 Then
                    If (VGA_GC(0) And 8) <> 0 Then
                        If (value And 8) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    curValue = LogicVGA(curValue, VGA_latch(3))
                    curValue = ((Not tmp) And curValue) Or (tmp And VGA_latch(3))
                    mVRAM(address + planeSize * 3) = curValue
                End If
        End Select
    End Sub

    Public Overrides Function Read(address As UInteger) As UInteger
        VGA_latch(0) = mVRAM(address + planeSize * 0)
        VGA_latch(1) = mVRAM(address + planeSize * 1)
        VGA_latch(2) = mVRAM(address + planeSize * 2)
        VGA_latch(3) = mVRAM(address + planeSize * 3)

        If (VGA_SC(2) And 1) <> 0 Then Return mVRAM(address + planeSize * 0)
        If (VGA_SC(2) And 2) <> 0 Then Return mVRAM(address + planeSize * 1)
        If (VGA_SC(2) And 4) <> 0 Then Return mVRAM(address + planeSize * 2)
        If (VGA_SC(2) And 8) <> 0 Then Return mVRAM(address + planeSize * 3)

        Return 0
    End Function

    Private Function ShiftVGA(value As UInteger) As UInteger
        For i As Integer = 0 To (VGA_GC(3) And 7) - 1
            value = (value >> 1) Or ((value And 1) << 7)
        Next
        Return value
    End Function

    Private Function LogicVGA(curValue As UInteger, latchValue As UInteger) As UInteger
        Select Case (VGA_GC(3) >> 3) And 3
            Case 1 : curValue = curValue And latchValue
            Case 2 : curValue = curValue Or latchValue
            Case 3 : curValue = curValue Xor latchValue
        End Select
        Return curValue
    End Function
End Class
