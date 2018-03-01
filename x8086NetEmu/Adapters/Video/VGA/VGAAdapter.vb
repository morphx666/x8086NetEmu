Imports System.Threading

Public MustInherit Class VGAAdapter
    Inherits CGAAdapter

    Public Shadows Enum VideoModes
        Mode0_Text_BW_40x25 = &H4
        Mode1_Text_Color_40x25 = &H0
        Mode2_Text_BW_80x25 = &H5
        Mode3_Text_Color_80x25 = &H1

        Mode4_Graphic_Color_320x200 = &H2
        Mode5_Graphic_BW_320x200 = &H6
        Mode6_Graphic_Color_640x200 = &H16
        Mode6_Graphic_Color_640x200_Alt = &H12

        Undefined = &HFF
    End Enum

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

    Private VRAM(&H40000) As Byte
    Private VGA_SC(&H100 - 1) As Byte
    Private VGA_CRTC(&H100 - 1) As Byte
    Private VGA_ATTR(&H100 - 1) As Byte
    Private VGA_GC(&H100 - 1) As Byte
    Private flip3C0 As Boolean
    Private latchRGB As Integer = 0
    Private latchPal As Integer = 0
    Private VGA_latch(4 - 1) As Byte
    Private stateDAC As Integer = 0
    Private latchReadRGB As Integer = 0
    Private latchReadPal As Integer = 0
    Private portRAM(&H3DF - &H3C0 - 1) As Byte
    Private tempRGB As Integer
    Protected VGAPalette(VGABasePalette.Length - 1) As Integer

    Private vgapage As Integer
    Private curPos As Integer
    Private curVisible As Integer
    Private vtotal As Integer
    Private port3DA As Integer

    Protected lockObject As New Object()

    Private mCursorCol As Integer = 0
    Private mCursorRow As Integer = 0
    Private mCursorVisible As Boolean
    Private mCursorStart As Integer = 0
    Private mCursorEnd As Integer = 1

    Private mVideoEnabled As Boolean = True
    Private mVideoMode As VideoModes = VideoModes.Undefined
    Private mBlinkRate As Integer = 16 ' 8 frames on, 8 frames off (http://www.oldskool.org/guides/oldonnew/resources/cgatech.txt)
    Private mBlinkCharOn As Boolean
    Private mPixelsPerByte As Integer

    Private mZoom As Double = 1.0

    Private mCPU As X8086

    Private lastInt10AX As Integer

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)
        mCPU = cpu

        mCPU.LoadBIN("roms\ET4000.BIN", &HC000, &H0)
        'mCPU.LoadBIN("..\..\Other Emulators & Resources\PCemV0.7\roms\TRIDENT.BIN", &HC000, &H0)
        'mCPU.LoadBIN("..\..\Other Emulators & Resources\xtbios31\test\ET4000.BIN", &HC000, &H0)
        'mCPU.LoadBIN("..\..\Other Emulators & Resources\fake86-0.12.9.19-win32\Binaries\videorom.bin", &HC000, &H0)

        'ValidPortAddress.Clear()
        For i As UInteger = &H3C0 To &H3DF
            ValidPortAddress.Add(i)
        Next

        For i As Integer = 0 To VGABasePalette.Length - 1
            VGAPalette(i) = VGABasePalette(i).ToArgb()
        Next

        mCPU.TryAttachHook(&H10, New X8086.IntHandler(Function()
                                                          If mCPU.Registers.AH = 0 OrElse mCPU.Registers.AH = &H10 Then
                                                              Dim AX As UInteger = mCPU.Registers.AX
                                                              VideoMode = mCPU.Registers.AX
                                                              mCPU.Registers.AX = AX
                                                              If mCPU.Registers.AH = &H10 Then Return False
                                                              If mVideoMode = 9 Then Return False
                                                          End If

                                                          If mCPU.Registers.AH = &H1A AndAlso lastInt10AX <> &H100 Then
                                                              mCPU.Registers.AL = &H1A
                                                              mCPU.Registers.AL = &H8
                                                              Return False
                                                          End If

                                                          lastInt10AX = mCPU.Registers.AX
                                                          Return False
                                                      End Function))
    End Sub

    Private vidColor As Integer
    Private vidGfxMode As Integer
    Private blankAttr As Integer

    Public Overrides Property VideoMode As UInteger
        Get
            Return mVideoMode
        End Get
        Set(value As UInteger)
            mVideoMode = value And &H7F

            Select Case (value And &HFF00) >> 8
                Case 0 ' Set video mode
                    VGA_SC(&H4) = 0
                    Select Case mVideoMode
                        Case 0 ' 40x25 Mono Text
                            mStartTextVideoAddress = &HB800
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(360, 400)
                            mMainMode = MainModes.Text
                            vidColor = 0
                            vidGfxMode = 0
                            blankAttr = 7
                            For i As Integer = mStartTextVideoAddress To mStartTextVideoAddress + &H4000 - 1 Step 2
                                mCPU.RAM(i) = 0
                                mCPU.RAM(i + 1) = blankAttr
                            Next

                        Case 1 ' 40x25 Color Text
                            mStartTextVideoAddress = &HB800
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(360, 400)
                            mMainMode = MainModes.Text
                            vidColor = 1
                            vidGfxMode = 0
                            blankAttr = 7
                            For i As Integer = mStartTextVideoAddress To mStartTextVideoAddress + &H4000 - 1 Step 2
                                mCPU.RAM(i) = 0
                                mCPU.RAM(i + 1) = blankAttr
                            Next
                            portRAM(&H3D8 - &H3C0) = portRAM(&H3D8 - &H3C0) And &HFE

                        Case 2 ' 80x25 Mono Text
                            mStartTextVideoAddress = &HB800
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(720, 400)
                            mMainMode = MainModes.Text
                            vidColor = 0
                            vidGfxMode = 0
                            blankAttr = 7
                            For i As Integer = mStartTextVideoAddress To mStartTextVideoAddress + &H4000 - 1 Step 2
                                mCPU.RAM(i) = 0
                                mCPU.RAM(i + 1) = blankAttr
                            Next
                            portRAM(&H3D8 - &H3C0) = portRAM(&H3D8 - &H3C0) And &HFE

                        Case 3 ' 80x25 Color Text
                            mStartTextVideoAddress = &HB800
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(720, 400)
                            mMainMode = MainModes.Text
                            vidColor = 1
                            vidGfxMode = 0
                            blankAttr = 7
                            For i As Integer = mStartTextVideoAddress To mStartTextVideoAddress + &H4000 - 1 Step 2
                                mCPU.RAM(i) = 0
                                mCPU.RAM(i + 1) = blankAttr
                            Next
                            portRAM(&H3D8 - &H3C0) = portRAM(&H3D8 - &H3C0) And &HFE

                        Case 4, 5 ' 320x200 4 Colors
                            mStartTextVideoAddress = &HB800
                            mStartGraphicsVideoAddress = &HB800
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(320, 200)
                            mMainMode = MainModes.Graphics
                            vidColor = 1
                            vidGfxMode = 1
                            blankAttr = 7
                            For i As Integer = mStartTextVideoAddress To mStartTextVideoAddress + &H4000 - 1 Step 2
                                mCPU.RAM(i) = 0
                                mCPU.RAM(i + 1) = blankAttr
                            Next
                            If value And &HF = 4 Then
                                portRAM(&H3D9 - &H3C0) = 48
                            Else
                                portRAM(&H3D9 - &H3C0) = 0
                            End If

                        Case 6 ' 640x200 2 Colors
                            mStartTextVideoAddress = &HB800
                            mStartGraphicsVideoAddress = &HB800
                            mTextResolution = New Size(80, 25)
                            mVideoResolution = New Size(640, 200)
                            mMainMode = MainModes.Graphics
                            vidColor = 0
                            vidGfxMode = 1
                            blankAttr = 7
                            For i As Integer = mStartTextVideoAddress To mStartTextVideoAddress + &H4000 - 1 Step 2
                                mCPU.RAM(i) = 0
                                mCPU.RAM(i + 1) = blankAttr
                            Next
                            portRAM(&H3D8 - &H3C0) = portRAM(&H3D8 - &H3C0) And &HFE

                        Case 9 ' 320x200 16 Colors
                            mStartTextVideoAddress = &HB800
                            mStartGraphicsVideoAddress = &HB800
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = New Size(320, 200)
                            mMainMode = MainModes.Graphics
                            vidColor = 1
                            vidGfxMode = 1
                            blankAttr = 0
                            If (value And &H80) = 0 Then
                                For i As Integer = mStartTextVideoAddress To mStartTextVideoAddress + &H10000 - 1 Step 2
                                    mCPU.RAM(i) = 0
                                    mCPU.RAM(i + 1) = blankAttr
                                Next
                            End If
                            portRAM(&H3D8 - &H3C0) = portRAM(&H3D8 - &H3C0) And &HFE

                        Case &HD, &H12, &H13 ' 320x200 16/256 Colors
                            mStartGraphicsVideoAddress = &HA0000
                            mTextResolution = New Size(40, 25)
                            mVideoResolution = If(mVideoMode = &H12, New Size(640, 480), New Size(320, 200))
                            mMainMode = MainModes.Graphics
                            vidColor = 1
                            vidGfxMode = 1
                            blankAttr = 0
                            For i As Integer = mStartTextVideoAddress To mStartTextVideoAddress + &H10000 - 1 Step 2
                                mCPU.RAM(i) = 0
                                mCPU.RAM(i + 1) = blankAttr
                            Next
                            portRAM(&H3D8 - &H3C0) = portRAM(&H3D8 - &H3C0) And &HFE

                        Case 127 ' 90x25 Mono Text
                            mStartTextVideoAddress = &HB800
                            mTextResolution = New Size(90, 25)
                            mVideoResolution = New Size(0, 0)
                            mMainMode = MainModes.Text
                            vidColor = 0
                            vidGfxMode = 1
                            For i As Integer = mStartTextVideoAddress To mStartTextVideoAddress + &H4000 - 1
                                mCPU.RAM(i) = 0
                            Next
                            portRAM(&H3D8 - &H3C0) = portRAM(&H3D8 - &H3C0) And &HFE

                    End Select

                    mCPU.RAM(&H449) = mVideoMode
                    mCPU.RAM(&H44A) = mTextResolution.Width
                    mCPU.RAM(&H44B) = 0
                    mCPU.RAM(&H484) = mTextResolution.Height

                    mCursorCol = 0
                    mCursorRow = 0

                    If (value And &H80) = 0 Then
                        Array.Clear(mCPU.Memory, &HA0000, &H1FFFF)
                        Array.Clear(VRAM, 0, VRAM.Length)
                    End If

                Case &H10 ' VGA DAC functions
                    Select Case value And &HFF
                        Case &H10 ' Set individual DAC register
                            VGAPalette(mCPU.Registers.BX) = RGBToUint((mCPU.Registers.DH And 63) << 2,
                                                                      (mCPU.Registers.CH And 63) << 2,
                                                                      (mCPU.Registers.CL And 63) << 2)
                        Case &H12 ' Set block of DAC registers
                            Dim addr As Integer = mCPU.Registers.ES * 16 + mCPU.Registers.DX
                            For n As Integer = mCPU.Registers.BX To mCPU.Registers.BX + mCPU.Registers.CX
                                VGAPalette(n) = RGBToUint(mCPU.RAM(addr + 0) << 2,
                                                          mCPU.RAM(addr + 1) << 2,
                                                          mCPU.RAM(addr + 2) << 2)
                            Next

                    End Select
                    OnPaletteRegisterChanged()

                Case &H1A ' Get display combination code (ps, vga/mcga)
                    mCPU.Registers.AL = &H1A
                    mCPU.Registers.AH = &H8
            End Select

            InitVideoMemory(False)
        End Set
    End Property

    Public Sub Write(address As UInteger, value As Byte)
        Dim planeSize As Integer = &H10000
        value = ShiftVGA(value)

        Select Case VGA_GC(5) And 3
            Case 0

        End Select
    End Sub

    Private Function ShiftVGA(value As Byte) As Byte
        For cnt As Integer = 0 To VGA_GC(3) And 7 - 1
            value = (value >> 1) Or ((value And 1) << 7)
        Next
        Return value
    End Function

    Private Function RGBToUint(r As Byte, g As Byte, b As Byte) As UInteger
        Return r Or (g << 8) Or (b << 16)
    End Function

    Private lastScanLineTick As Long
    Private scanLineTiming As Long = Scheduler.CLOCKRATE / (31500 * X8086.MHz)
    Private curScanLine As Long

    Public Overrides Function [In](port As UInteger) As UInteger
        Select Case port
            Case &H3C1
                Return VGA_ATTR(portRAM(&H3C0 - &H3C0))

            Case &H3C5
                Return VGA_ATTR(portRAM(&H3C4 - &H3C0))

            Case &H3D5
                Return VGA_ATTR(portRAM(&H3D4 - &H3C0))

            Case &H3C7
                Return stateDAC

            Case &H3C8
                Return latchReadPal

            Case &H3C9
                Select Case latchReadRGB
                    Case 0 ' R
                        Return (VGAPalette(latchReadPal) >> 2) And 63
                    Case 1 ' G
                        Return (VGAPalette(latchReadPal) >> 10) And 63
                    Case 2 ' B
                        latchReadRGB = 0
                        Dim b As Integer = (VGAPalette(latchReadPal) >> 18) And 63
                        latchReadPal += 1
                        Return b
                End Select

            Case &H3DA
                Dim t As Long = mCPU.Sched.CurrentTime
                If t >= (lastScanLineTick + scanLineTiming) Then
                    curScanLine = (curScanLine + 1) Mod 525
                    If curScanLine > 479 Then
                        port3DA = 8
                    ElseIf (curScanLine And 1) <> 0 Then
                        port3DA = port3DA Or 1
                    End If
                    lastScanLineTick = t
                End If
                Return port3DA

                'Case &H3D0 To &H3DF
                '    Return MyBase.In(port)

        End Select

        Return portRAM(port - &H3C0)
    End Function

    Public Overrides Sub Out(port As UInteger, value As UInteger)
        Dim ramAddr As Integer = port - &H3C0
        value = value And &HFF

        Select Case port
            Case &H3B8
                If (value And 2) = 2 AndAlso VideoMode <> 127 Then
                    Dim AX As UInteger = mCPU.Registers.AX
                    mCPU.Registers.AX = 127
                    VideoMode = mCPU.Registers.AX
                    mCPU.Registers.AX = AX
                End If
                If (value And &H80) <> 0 Then
                    mStartTextVideoAddress = &HB8000
                Else
                    mStartTextVideoAddress = &HB0000
                End If
            Case &H3C0
                If flip3C0 Then
                    flip3C0 = False
                    portRAM(ramAddr) = value And &HFF
                Else
                    flip3C0 = True
                    VGA_ATTR(ramAddr) = value And &HFF
                End If

            Case &H3C4 ' Sequence controller index
                portRAM(ramAddr) = value And &HFF

            Case &H3C5 ' Sequence controller data
                VGA_SC(portRAM(ramAddr)) = value And &HFF

            Case &H3D4 ' CRT controller index
                portRAM(ramAddr) = value And &HFF

            Case &H3C7 ' Color index register (read operations)
                latchReadPal = value And &HFF
                latchReadRGB = 0
                stateDAC = 0

            Case &H3C8 ' Color index register (write operations)
                latchPal = value And &HFF
                latchRGB = 0
                tempRGB = 0
                stateDAC = 3

            Case &H3C9 ' RGB data register
                value = value And 63
                Select Case latchRGB
                    Case 0 ' R
                        tempRGB = value << 2
                    Case 1 ' G
                        tempRGB = tempRGB Or (value << 10)
                    Case 2 ' B
                        tempRGB = tempRGB Or (value << 18)
                        'VGAPalette(latchPal) = tempRGB
                        latchPal += 1
                End Select
                latchRGB = (latchRGB + 1) Mod 3

            Case &H3D5 ' Cursor position latch
                VGA_CRTC(portRAM(&H3D4 - &H3C0)) = value And &HFF
                If portRAM(&H3D4 - &H3C0) = &HE Then
                    curPos = (curPos And &HFF) Or (value << 8)
                ElseIf portRAM(&H3D4 - &H3C0) = &HF Then
                    curPos = (curPos And &HFF00) Or value
                End If

                mCursorRow = curPos / mTextResolution.Width
                mCursorCol = curPos Mod mTextResolution.Width

                If portRAM(&H3D4 - &H3C0) = &H6 Then
                    vtotal = value Or ((VGA_GC(7) And 1) << 8) Or (If(VGA_GC(7) And 32 <> 0, 1, 0) << 9)
                End If

            Case &H3CF
                VGA_GC(portRAM(&H3CE - &H3C0)) = value

                'Case &H3D0 To &H3DF
                '    MyBase.Out(port, value)

            Case Else
                portRAM(ramAddr) = value

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

        MyBase.InitVideoMemory(clearScreen)

        X8086.Notify("Set Video Mode: {0} @ {1}", X8086.NotificationReasons.Info, mVideoMode, videoTextSegment.ToHex(X8086.DataSize.Word))

        mStartTextVideoAddress = X8086.SegmentOffetToAbsolute(videoTextSegment, 0)
        mEndTextVideoAddress = mStartTextVideoAddress + &H4000

        mStartGraphicsVideoAddress = X8086.SegmentOffetToAbsolute(videoGraphicsSegment, 0)
        mEndGraphicsVideoAddress = mStartGraphicsVideoAddress + &H4000

        mPixelsPerByte = If(VideoMode = VideoModes.Mode6_Graphic_Color_640x200, 8, 4)

        OnPaletteRegisterChanged()
        AutoSize()
    End Sub
End Class
