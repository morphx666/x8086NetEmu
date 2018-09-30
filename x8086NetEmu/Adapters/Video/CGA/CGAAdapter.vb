Imports System.Threading

Public MustInherit Class CGAAdapter
    Inherits VideoAdapter

    Public Const VERTSYNC As Double = 60.0
    Public Const HORIZSYNC As Double = VERTSYNC * 262.5

    Protected Const ht As Long = Scheduler.BASECLOCK \ HORIZSYNC
    Protected Const vt As Long = (Scheduler.BASECLOCK \ HORIZSYNC) * (HORIZSYNC \ VERTSYNC)

    Public Enum VideoModes
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

    Private CGABasePalette() As Color = {
        Color.FromArgb(&H0, &H0, &H0),
        Color.FromArgb(&H0, &H0, &HAA),
        Color.FromArgb(&H0, &HAA, &H0),
        Color.FromArgb(&H0, &HAA, &HAA),
        Color.FromArgb(&HAA, &H0, &H0),
        Color.FromArgb(&HAA, &H0, &HAA),
        Color.FromArgb(&HAA, &H55, &H0),
        Color.FromArgb(&HAA, &HAA, &HAA),
        Color.FromArgb(&H55, &H55, &H55),
        Color.FromArgb(&H55, &H55, &HFF),
        Color.FromArgb(&H55, &HFF, &H55),
        Color.FromArgb(&H55, &HFF, &HFF),
        Color.FromArgb(&HFF, &H55, &H55),
        Color.FromArgb(&HFF, &H55, &HFF),
        Color.FromArgb(&HFF, &HFF, &H55),
        Color.FromArgb(&HFF, &HFF, &HFF)
    }

    ' http://www.htl-steyr.ac.at/~morg/pcinfo/hardware/interrupts/inte6l9s.htm

    Protected CGAPalette(16 - 1) As Color

    Protected Friend Enum CGAModeControlRegisters
        blink_enabled = 5
        high_resolution_graphics = 4
        video_enabled = 3
        black_and_white = 2
        graphics_mode = 1
        high_resolution = 0
    End Enum

    Protected Friend Enum CGAColorControlRegisters
        bright_background_or_blinking_text = 7
        red_background = 6
        green_background = 5
        blue_background = 4
        bright_foreground = 3
        red_foreground = 2
        green_foreground = 1
        blue_foreground = 0
    End Enum

    Protected Friend Enum CGAStatusRegisters
        vertical_retrace = 3
        light_pen_switch_status = 2
        light_pen_trigger_set = 1
        display_enable = 0
    End Enum

    Protected Friend Enum CGAPaletteRegisters
        active_color_set_is_red_green_brown = 5
        intense_colors_in_graphics_or_background_colors_text = 4
        intense_border_in_40x25_or_intense_background_in_320x200_or_intense_foreground_in_640x200 = 3
        red_border_in_40x25_or_red_background_in_320x200_or_red_foreground_in_640x200 = 2
        green_border_in_40x25_or_green_background_in_320x200_or_green_foreground_in_640x200 = 2
        blue_border_in_40x25_or_blue_background_in_320x200_or_blue_foreground_in_640x200 = 0
    End Enum

    Private ReadOnly CtrlMask() As Byte = {
        &HFF, &HFF, &HFF, &HFF, &H7F, &H1F, &H7F, &H7F, &HF3, &H1F, &H7F, &H1F, &H3F, &HFF, &H3F, &HFF,
        &HFF, &HFF, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0
    }

    Protected Friend CRT6845IndexRegister As Byte = 0
    Protected Friend CRT6845DataRegister(256 - 1) As Byte

    Protected Friend CGAModeControlRegister(8 - 1) As Boolean
    Protected Friend CGAColorControlRegister(8 - 1) As Boolean
    Protected Friend CGAStatusRegister(8 - 1) As Boolean
    Protected Friend CGAPaletteRegister(8 - 1) As Boolean

    Protected isInit As Boolean

    Protected mCursorCol As Integer = 0
    Protected mCursorRow As Integer = 0
    Protected mCursorVisible As Boolean
    Protected mCursorStart As Integer = 0
    Protected mCursorEnd As Integer = 1

    Protected mVideoEnabled As Boolean = True
    Protected mVideoMode As UInt32 = VideoModes.Undefined
    Protected mBlinkRate As Integer = 16 ' 8 frames on, 8 frames off (http://www.oldskool.org/guides/oldonnew/resources/cgatech.txt)
    Protected mBlinkCharOn As Boolean
    Protected mPixelsPerByte As Integer

    Private mZoom As Double = 1.0

    Protected videoBMP As DirectBitmap = New DirectBitmap(1, 1)
    Private waiter As AutoResetEvent
    Protected cancelAllThreads As Boolean
    Private ReadOnly useInternalTimer As Boolean

    'Public Event VideoRefreshed(sender As Object)

    Protected chars(256 - 1) As Char

    Private mCPU As X8086
    Protected vidModeChangeFlag As Integer = &B1000

    Public MustOverride Overrides Sub AutoSize()
    Protected MustOverride Sub Render()

    Public Sub New(cpu As X8086, Optional useInternalTimer As Boolean = True)
        MyBase.New(cpu)
        mCPU = cpu
        Me.useInternalTimer = useInternalTimer

        For i As UInt32 = &H3D0 To &H3DF
            ValidPortAddress.Add(i)
        Next

        For i As Integer = 0 To 255
            If i >= 32 AndAlso i < 255 Then
                chars(i) = Convert.ToChar(i)
            Else
                chars(i) = " "
            End If
        Next

        waiter = New AutoResetEvent(False)
        Reset()
    End Sub

    Public Sub HandleKeyDown(sender As Object, e As KeyEventArgs)
        MyBase.OnKeyDown(Me, e)
        If e.Handled Then Exit Sub
        'Debug.WriteLine($"KEY DOWN: {e.KeyCode} | {e.Modifiers} | {e.KeyValue}")
        If mCPU.Keyboard IsNot Nothing Then mCPU.Sched.HandleInput(New ExternalInputEvent(mCPU.Keyboard, e, False))
        e.Handled = True
        e.SuppressKeyPress = True
    End Sub

    Public Sub HandleKeyUp(sender As Object, e As KeyEventArgs)
        MyBase.OnKeyUp(Me, e)
        If e.Handled Then Exit Sub
        'Debug.WriteLine($"KEY UP: {e.KeyCode} | {e.Modifiers} | {e.KeyValue}")
        If mCPU.Keyboard IsNot Nothing Then mCPU.Sched.HandleInput(New ExternalInputEvent(mCPU.Keyboard, e, True))
        e.Handled = True
    End Sub

    Public Sub OnMouseDown(sender As Object, e As MouseEventArgs)
        If mCPU.Mouse IsNot Nothing Then mCPU.Sched.HandleInput(New ExternalInputEvent(mCPU.Mouse, e, True))
    End Sub

    Public Sub OnMouseMove(sender As Object, e As MouseEventArgs)
        If mCPU.Mouse IsNot Nothing Then mCPU.Sched.HandleInput(New ExternalInputEvent(mCPU.Mouse, e, Nothing))
    End Sub

    Public Sub OnMouseUp(sender As Object, e As MouseEventArgs)
        If mCPU.Mouse IsNot Nothing Then mCPU.Sched.HandleInput(New ExternalInputEvent(mCPU.Mouse, e, False))
    End Sub

    Public ReadOnly Property BlinkRate As Integer
        Get
            Return mBlinkRate
        End Get
    End Property

    Public ReadOnly Property BlinkCharOn As Boolean
        Get
            Return mBlinkCharOn
        End Get
    End Property

    Public ReadOnly Property CursorStart As Integer
        Get
            Return mCursorStart
        End Get
    End Property

    Public ReadOnly Property CursorEnd As Integer
        Get
            Return mCursorEnd
        End Get
    End Property

    Public ReadOnly Property VideoEnabled As Boolean
        Get
            Return mVideoEnabled
        End Get
    End Property

    Public ReadOnly Property PixelsPerByte As Integer
        Get
            Return mPixelsPerByte
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "CGA"
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "CGA (6845) Emulator"
        End Get
    End Property

    Public Overrides ReadOnly Property Vendor As String
        Get
            Return "xFX JumpStart"
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMajor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMinor As Integer
        Get
            Return 4
        End Get
    End Property

    Public Overrides ReadOnly Property VersionRevision As Integer
        Get
            Return 7
        End Get
    End Property

    Public Overrides ReadOnly Property Type As Adapter.AdapterType
        Get
            Return AdapterType.Video
        End Get
    End Property

    Public ReadOnly Property CursorCol As Integer
        Get
            Return mCursorCol
        End Get
    End Property

    Public ReadOnly Property CursorRow As Integer
        Get
            Return mCursorRow
        End Get
    End Property

    Public Overrides Sub InitiAdapter()
        isInit = mCPU IsNot Nothing
        If isInit AndAlso useInternalTimer Then Tasks.Task.Run(Sub() MainLoop())
    End Sub

    Private Sub MainLoop()
        Do
            waiter.WaitOne(2 * 1000 \ VERTSYNC)

            Render()

            'RaiseEvent VideoRefreshed(Me)
        Loop Until cancelAllThreads
    End Sub

    Public Overrides Sub Reset()
        X8086.WordToBitsArray(&H29, CGAModeControlRegister)
        X8086.WordToBitsArray(&H0, CGAColorControlRegister)
        CRT6845DataRegister(0) = &H71
        CRT6845DataRegister(1) = &H50
        CRT6845DataRegister(2) = &H5A
        CRT6845DataRegister(3) = &HA
        CRT6845DataRegister(4) = &H1F
        CRT6845DataRegister(5) = &H6
        CRT6845DataRegister(6) = &H19
        CRT6845DataRegister(7) = &H1C
        CRT6845DataRegister(8) = &H2
        CRT6845DataRegister(9) = &H7
        CRT6845DataRegister(10) = &H6
        CRT6845DataRegister(11) = &H71
        For i As Integer = 12 To 32 - 1
            CRT6845DataRegister(i) = 0
        Next

        'HandleCGAModeControlRegisterUpdated()
        InitVideoMemory(False)
    End Sub

    Public ReadOnly Property CursorVisible As Boolean
        Get
            Return mCursorVisible
        End Get
    End Property

    Public ReadOnly Property CursorLocation As Point
        Get
            Return New Point(mCursorCol, mCursorRow)
        End Get
    End Property

    Public Overrides Property VideoMode As UInt32
        Get
            Return mVideoMode
        End Get
        Set(value As UInt32)
            mVideoMode = value And (Not &H80)

            mStartTextVideoAddress = &HB8000
            mStartGraphicsVideoAddress = &HB8000

            Select Case value
                Case VideoModes.Mode0_Text_BW_40x25
                    mTextResolution = New Size(40, 25)
                    mVideoResolution = New Size(0, 0)
                    mMainMode = MainModes.Text

                Case VideoModes.Mode1_Text_Color_40x25
                    mTextResolution = New Size(40, 25)
                    mVideoResolution = New Size(0, 0)
                    mMainMode = MainModes.Text

                Case VideoModes.Mode2_Text_BW_80x25
                    mTextResolution = New Size(80, 25)
                    mVideoResolution = New Size(0, 0)
                    mMainMode = MainModes.Text

                Case VideoModes.Mode3_Text_Color_80x25
                    mTextResolution = New Size(80, 25)
                    mVideoResolution = New Size(0, 0)
                    mMainMode = MainModes.Text

                Case VideoModes.Mode4_Graphic_Color_320x200
                    mTextResolution = New Size(40, 25)
                    mVideoResolution = New Size(320, 200)
                    mMainMode = MainModes.Graphics

                Case VideoModes.Mode5_Graphic_BW_320x200
                    mTextResolution = New Size(40, 25)
                    mVideoResolution = New Size(320, 200)
                    mMainMode = MainModes.Graphics

                Case VideoModes.Mode6_Graphic_Color_640x200, VideoModes.Mode6_Graphic_Color_640x200_Alt
                    mTextResolution = New Size(80, 25)
                    mVideoResolution = New Size(640, 200)
                    mMainMode = MainModes.Graphics

                Case Else
                    mCPU.RaiseException("CGA: Unknown Video Mode " + CInt(value).ToString("X2"))
                    mVideoMode = VideoModes.Undefined
            End Select

            InitVideoMemory(False)
        End Set
    End Property

    Protected Overridable Sub InitVideoMemory(clearScreen As Boolean)
        If Not isInit Then Exit Sub

        mEndTextVideoAddress = mStartTextVideoAddress + &H4000
        mEndGraphicsVideoAddress = mStartGraphicsVideoAddress + &H4000

        mPixelsPerByte = If(VideoMode = VideoModes.Mode6_Graphic_Color_640x200, 8, 4)

        X8086.Notify("Set Video Mode: {0}", X8086.NotificationReasons.Info, mVideoMode)

        OnPaletteRegisterChanged()

        AutoSize()
    End Sub

    Public Overrides Function [In](port As UInt32) As UInt16
        Select Case port
            Case &H3D0, &H3D2, &H3D4, &H3D6 ' CRT (6845) index register
                Return CRT6845IndexRegister

            Case &H3D1, &H3D3, &H3D5, &H3D7 ' CRT (6845) data register
                Return CRT6845DataRegister(CRT6845IndexRegister)

            Case &H3D8 ' CGA mode control register  (except PCjr)
                Return X8086.BitsArrayToWord(CGAModeControlRegister)

            Case &H3D9 ' CGA palette register
                Return X8086.BitsArrayToWord(CGAPaletteRegister)

            Case &H3DA ' CGA status register
                UpdateStatusRegister()
                Return X8086.BitsArrayToWord(CGAStatusRegister)

            Case &H3DF ' CRT/CPU page register  (PCjr only)
#If DEBUG Then
                'stop
#End If
            Case Else
                mCPU.RaiseException("CGA: Unknown In Port: " + port.ToString("X4"))
        End Select

        Return &HFF
    End Function

    Public Overrides Sub Out(port As UInt32, value As UInt16)
        Select Case port
            Case &H3D0, &H3D2, &H3D4, &H3D6 ' CRT (6845) index register
                CRT6845IndexRegister = value And 31

            Case &H3D1, &H3D3, &H3D5, &H3D7 ' CRT (6845) data register
                CRT6845DataRegister(CRT6845IndexRegister) = value And CtrlMask(CRT6845IndexRegister)
                OnDataRegisterChanged()

            Case &H3D8 ' CGA mode control register  (except PCjr)
                X8086.WordToBitsArray(value, CGAModeControlRegister)
                OnModeControlRegisterChanged()

            Case &H3D9 ' CGA palette register
                X8086.WordToBitsArray(value, CGAPaletteRegister)
                OnPaletteRegisterChanged()

            Case &H3DA ' CGA status register	EGA/VGA: input status 1 register / EGA/VGA feature control register
                X8086.WordToBitsArray(value, CGAStatusRegister)

            Case &H3DB ' The trigger is cleared by writing any value to port 03DBh (undocumented)
                CGAStatusRegister(CGAStatusRegisters.light_pen_trigger_set) = False

            Case &H3DF ' CRT/CPU page register  (PCjr only)
                'Stop

            Case Else
                mCPU.RaiseException("CGA: Unknown Out Port: " + port.ToString("X4"))
        End Select
    End Sub

    Protected Overridable Sub OnDataRegisterChanged()
        mCursorVisible = (CRT6845DataRegister(&HA) And &H60) <> &H20

        If mCursorVisible Then
            Dim startOffset As Integer = ((CRT6845DataRegister(&HC) And &H3F) << 8) Or (CRT6845DataRegister(&HD) And &HFF)
            Dim p As Integer = ((CRT6845DataRegister(&HE) And &H3F) << 8) Or (CRT6845DataRegister(&HF) And &HFF)
            p = (p - startOffset) And &H1FFF
            If p < 0 Then
                mCursorCol = 0
                mCursorRow = 50
            Else
                mCursorCol = p Mod mTextResolution.Width
                mCursorRow = p \ mTextResolution.Width
            End If
        End If

        mCursorStart = CRT6845DataRegister(&HA) And &B11111
        mCursorEnd = CRT6845DataRegister(&HB) And &B11111

        mBlinkCharOn = CGAModeControlRegister(CGAModeControlRegisters.blink_enabled)
    End Sub

    Protected Overridable Sub OnModeControlRegisterChanged()
        ' http://www.seasip.info/VintagePC/cga.html
        Dim v As UInt32 = X8086.BitsArrayToWord(CGAModeControlRegister)
        Dim newMode As VideoModes = CType(v And &H17, VideoModes) ' 10111

        If (v And vidModeChangeFlag) <> 0 AndAlso newMode <> mVideoMode Then VideoMode = newMode

        mVideoEnabled = CGAModeControlRegister(CGAModeControlRegisters.video_enabled)
    End Sub

    Protected Overridable Sub OnPaletteRegisterChanged()
        If MainMode = MainModes.Text Then
            CGAPalette = CType(CGABasePalette.Clone(), Color())
        Else
            Dim colors() As Color = Nothing
            Dim cgaModeReg As UInt32 = X8086.BitsArrayToWord(CGAModeControlRegister)
            Dim cgaColorReg As UInt32 = X8086.BitsArrayToWord(CGAPaletteRegister)

            Select Case VideoMode
                Case VideoModes.Mode4_Graphic_Color_320x200
                    Dim intense As Integer = (cgaColorReg And &H10) >> 1
                    Dim pal1 As Integer = (cgaColorReg >> 5) And (Not cgaModeReg >> 2) And 1
                    Dim pal2 As Integer = ((Not cgaColorReg) >> 5) And (Not cgaModeReg >> 2) And 1

                    colors = New Color() {
                            CGABasePalette(cgaColorReg And &HF),
                            CGABasePalette(3 Xor pal2 Or intense),
                            CGABasePalette(4 Xor pal1 Or intense),
                            CGABasePalette(7 Xor pal2 Or intense)
                        }
                Case VideoModes.Mode6_Graphic_Color_640x200
                    colors = New Color() {
                            CGABasePalette(0),
                            CGABasePalette(cgaColorReg And &HF)
                        }
            End Select

            If colors IsNot Nothing Then
                For i As Integer = 0 To colors.Length - 1
                    CGAPalette(i) = colors(i)
                Next
            End If
        End If
    End Sub

    Private Sub UpdateStatusRegister()
        ' Determine current retrace state
        Dim t As Long = mCPU.Sched.CurrentTime
        Dim hRetrace As Boolean = (t Mod ht) <= (ht \ 10)
        Dim vRetrace As Boolean = (t Mod vt) <= (vt \ 10)

        CGAStatusRegister(CGAStatusRegisters.display_enable) = hRetrace
        CGAStatusRegister(CGAStatusRegisters.vertical_retrace) = vRetrace
    End Sub

    Public Overrides Property Zoom As Double
        Get
            Return mZoom
        End Get
        Set(value As Double)
            mZoom = value
            AutoSize()
        End Set
    End Property

    Public Overrides Sub CloseAdapter()
        isInit = False
        cancelAllThreads = True

        Application.DoEvents()
    End Sub
End Class