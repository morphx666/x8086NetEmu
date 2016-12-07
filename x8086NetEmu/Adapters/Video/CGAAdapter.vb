Imports System.Threading

Public MustInherit Class CGAAdapter
    Inherits Adapter

    Public Const VERTSYNC As Double = 60.0
    Public Const HORIZSYNC As Double = VERTSYNC * 262.5

    Private ht As Long = Scheduler.CLOCKRATE \ HORIZSYNC
    Private vt As Long = (Scheduler.CLOCKRATE \ HORIZSYNC) * (HORIZSYNC \ VERTSYNC)

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

    Public Enum MainModes
        Unknown = -1
        Text = 0
        Graphics = 2
    End Enum

    Public CGABasePalette() As Color = {
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

    Protected Friend CRT6845IndexRegister As Byte = 0
    Protected Friend CRT6845DataRegister(256 - 1) As Byte

    Protected Friend CGAModeControlRegister(8 - 1) As Boolean
    Protected Friend CGAColorControlRegister(8 - 1) As Boolean
    Protected Friend CGAStatusRegister(8 - 1) As Boolean
    Protected Friend CGAPaletteRegister(8 - 1) As Boolean

    Protected isInit As Boolean

    Private videoTextSegment As Integer = &HB800
    Private videoGraphicsSegment As Integer = &HB800

    Private mStartTextVideoAddress As Integer
    Private mEndTextVideoAddress As Integer

    Private mStartGraphicsVideoAddress As Integer
    Private mEndGraphicsVideoAddress As Integer

    Private mTextResolution As Size = New Size(40, 25)
    Private mVideoResolution As Size = New Size(0, 0)

    Private mCursorCol As Integer = 0
    Private mCursorRow As Integer = 0
    Private mCursorVisible As Boolean

    Private mVideoEnabled As Boolean = True
    Private mVideoMode As VideoModes = VideoModes.Undefined
    Private mMainMode As MainModes
    Private mBlinkRate As Integer = 16 ' 8 frames on, 8 frames off (http://www.oldskool.org/guides/oldonnew/resources/cgatech.txt)
    Private mBlinkCharOn As Boolean

    Private mZoom As Double = 1.0

    Protected lockObject As New Object()

    Private loopThread As Thread
    Private waiter As AutoResetEvent
    Protected cancelAllThreads As Boolean
    Private useInternalTimer As Boolean

    'Public Event VideoRefreshed(sender As Object)

    Protected chars(256 - 1) As Char

    Private mCPU As x8086

    Protected MustOverride Sub Render()

    Public Event KeyDown(sender As Object, e As KeyEventArgs)
    Public Event KeyUp(sender As Object, e As KeyEventArgs)

    Public Sub New(cpu As x8086, Optional useInternalTimer As Boolean = True)
        mCPU = cpu
        Me.useInternalTimer = useInternalTimer

        For i As Integer = &H3D0 To &H3DF
            ValidPortAddress.Add(i)
        Next

        'ValidPortAddress.Add(&H3B8)

        For i As Integer = 0 To 255
            If i >= 32 AndAlso i < 255 Then
                chars(i) = Chr(i)
            Else
                chars(i) = " "
            End If
        Next

        waiter = New AutoResetEvent(False)
        Reset()
    End Sub

    Public Sub OnKeyDown(sender As Object, e As KeyEventArgs)
        RaiseEvent KeyDown(Me, e)
        If e.Handled Then Exit Sub
        'Debug.WriteLine("KEY DOWN: " + e.KeyCode.ToString() + " | " + e.Modifiers.ToString())
        If mCPU.Keyboard IsNot Nothing Then mCPU.Sched.HandleInput(New ExternalInputEvent(mCPU.Keyboard, e, False))
        e.Handled = True
    End Sub

    Public Sub OnKeyUp(sender As Object, e As KeyEventArgs)
        RaiseEvent KeyUp(Me, e)
        If e.Handled Then Exit Sub
        'Debug.WriteLine("KEY UP:   " + e.KeyCode.ToString() + " | " + e.Modifiers.ToString())
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

    Public Overrides ReadOnly Property Name As String
        Get
            Return "CGA"
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "CGA Emulator"
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
            Return 3
        End Get
    End Property

    Public Overrides ReadOnly Property VersionRevision As Integer
        Get
            Return 2
        End Get
    End Property

    Public Overrides ReadOnly Property Type As Adapter.AdapterType
        Get
            Return AdapterType.Video
        End Get
    End Property

    Public ReadOnly Property StartGraphicsVideoAddress As Integer
        Get
            Return mStartGraphicsVideoAddress
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
        isInit = (mCPU IsNot Nothing)
        If isInit Then
            VideoMode = VideoModes.Mode3_Text_Color_80x25

            If useInternalTimer Then
                loopThread = New Thread(AddressOf MainLoop)
                loopThread.Start()
            End If
        End If
    End Sub

    Public Sub Update()
        waiter.Set()
    End Sub

    Private Sub MainLoop()
        Do
            waiter.WaitOne(1000 / VERTSYNC)
            'waiter.WaitOne()

            If isInit AndAlso mVideoEnabled AndAlso mVideoMode <> VideoModes.Undefined Then
                SyncLock lockObject
                    Render()
                End SyncLock
            End If

            'RaiseEvent VideoRefreshed(Me)
        Loop Until cancelAllThreads
    End Sub

    Public Sub Reset()
        x8086.WordToBitsArray(&H29, CGAModeControlRegister)
        x8086.WordToBitsArray(&H0, CGAColorControlRegister)
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

    Public ReadOnly Property TextResolution As Size
        Get
            Return mTextResolution
        End Get
    End Property

    Public ReadOnly Property VideoResolution As Size
        Get
            Return mVideoResolution
        End Get
    End Property

    Public ReadOnly Property StartTextVideoAddress As Integer
        Get
            Return mStartTextVideoAddress
        End Get
    End Property

    Public ReadOnly Property EndTextVideoAddress As Integer
        Get
            Return mEndTextVideoAddress
        End Get
    End Property

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

    Public ReadOnly Property MainMode As MainModes
        Get
            Return mMainMode
        End Get
    End Property

    Public Property VideoMode() As VideoModes
        Get
            Return mVideoMode
        End Get
        Set(value As VideoModes)
            Dim clearScreen As Boolean = False ' (value And &H80) OrElse (mVideoMode <> (value And (Not &H80)))
            mVideoMode = (value And (Not &H80))

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
                    mCPU.RaiseException("CGA: Unknown Video Mode " + CInt(value).ToHex(x8086.DataSize.Byte))
                    mVideoMode = VideoModes.Undefined
            End Select

            InitVideoMemory(clearScreen)
        End Set
    End Property

    Protected Overridable Sub InitVideoMemory(clearScreen As Boolean)
        If Not isInit Then Exit Sub

        x8086.Notify("Set Video Mode: {0} @ {1}", x8086.NotificationReasons.Info, mVideoMode, videoTextSegment.ToHex(x8086.DataSize.Word))

        mStartTextVideoAddress = x8086.SegOffToAbs(videoTextSegment, 0)
        mEndTextVideoAddress = mStartTextVideoAddress + &H4000

        mStartGraphicsVideoAddress = x8086.SegOffToAbs(videoGraphicsSegment, 0)
        mEndGraphicsVideoAddress = mStartGraphicsVideoAddress + &H4000

        OnPaletteRegisterChanged()
        AutoSize()
    End Sub

    Public Overrides Function [In](port As UInteger) As UInteger
        Select Case port
            Case &H3D0, &H3D2, &H3D4, &H3D6 ' CRT (6845) index register
                Return CRT6845IndexRegister

            Case &H3D1, &H3D3, &H3D5, &H3D7 ' CRT (6845) data register
                Return CRT6845DataRegister(CRT6845IndexRegister)

            Case &H3D8 ' CGA mode control register  (except PCjr)
                Return x8086.BitsArrayToWord(CGAModeControlRegister)

            Case &H3D9 ' CGA palette register
                Return x8086.BitsArrayToWord(CGAPaletteRegister)

            Case &H3DA ' CGA status register
                UpdateStatusRegister()
                Return x8086.BitsArrayToWord(CGAStatusRegister)

            Case &H3DF ' CRT/CPU page register  (PCjr only)
                Stop
            Case Else
                mCPU.RaiseException("CGA: Unknown In Port: " + port.ToHex(x8086.DataSize.Word))
        End Select

        Return &HFF
    End Function

    Public Overrides Sub Out(port As UInteger, value As UInteger)
        Select Case port
            'Case &H3B8
            '    If value And &H80 Then
            '        videoTextSegment = &HB800
            '    Else
            '        videoTextSegment = &HB000
            '    End If
            '    InitVideoMemory(True)

            Case &H3D0, &H3D2, &H3D4, &H3D6 ' CRT (6845) index register
                CRT6845IndexRegister = value And &HFF

            Case &H3D1, &H3D3, &H3D5, &H3D7 ' CRT (6845) data register
                CRT6845DataRegister(CRT6845IndexRegister) = value
                OnDataRegisterChanged()

            Case &H3D8 ' CGA mode control register  (except PCjr)
                'If videoTextSegment <> &HB800 Then
                '    videoTextSegment = &HB800
                '    InitVideoMemory(True)
                'End If
                x8086.WordToBitsArray(value, CGAModeControlRegister)
                OnModeControlRegisterChanged()

            Case &H3D9 ' CGA palette register
                x8086.WordToBitsArray(value, CGAPaletteRegister)
                OnPaletteRegisterChanged()

            Case &H3DA ' CGA status register	EGA/VGA: input status 1 register / EGA/VGA feature control register
                x8086.WordToBitsArray(value, CGAStatusRegister)

            Case &H3DB ' The trigger is cleared by writing any value to port 03DBh (undocumented)
                CGAStatusRegister(CGAStatusRegisters.light_pen_trigger_set) = False

            Case &H3DF ' CRT/CPU page register  (PCjr only)
                Stop
            Case Else
                mCPU.RaiseException("CGA: Unknown Out Port: " + port.ToHex(x8086.DataSize.Word))
        End Select
    End Sub

    Protected Overridable Sub OnDataRegisterChanged()
        mCursorVisible = ((CRT6845DataRegister(&HA) And &H30) = 0)

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
    End Sub

    Private vidModeChangeFlag As New Binary("1000", Binary.Sizes.Byte)

    Protected Overridable Sub OnModeControlRegisterChanged()
        ' http://www.seasip.info/VintagePC/cga.html
        Dim v As Integer = x8086.BitsArrayToWord(CGAModeControlRegister)
        Dim newMode As VideoModes = v And &H17 ' 10111
        ' 00100101

        If (v And vidModeChangeFlag) <> 0 Then
            If newMode <> mVideoMode Then VideoMode = newMode
        End If

        mVideoEnabled = CGAModeControlRegister(CGAModeControlRegisters.video_enabled) <> 0
        mBlinkCharOn = CGAModeControlRegister(CGAModeControlRegisters.blink_enabled) <> 0
    End Sub

    Protected Overridable Sub OnPaletteRegisterChanged()
        If MainMode = MainModes.Text Then
            CGAPalette = CGABasePalette.Clone()
        Else
            Dim colors() As Color = Nothing
            Dim cgaModeReg As Integer = x8086.BitsArrayToWord(CGAModeControlRegister)
            Dim cgaColorReg As Integer = x8086.BitsArrayToWord(CGAPaletteRegister)

            'Dim burts As Boolean = (cgaModeReg And &H4) <> 0
            'Dim pal As Boolean = (cgaColorReg And &H20) <> 0
            'Dim int As Boolean = (cgaColorReg And &H10) <> 0

            'If burts Then
            '    colors = New Color() {
            '                CGABasePalette(cgaColorReg And &HF),
            '                CGABasePalette(3 + If(int, 8, 0)),
            '                CGABasePalette(4 + If(int, 8, 0)),
            '                CGABasePalette(7 + If(int, 8, 0))
            '            }
            'ElseIf pal Then
            '    colors = New Color() {
            '                CGABasePalette(cgaColorReg And &HF),
            '                CGABasePalette(3 + If(int, 8, 0)),
            '                CGABasePalette(5 + If(int, 8, 0)),
            '                CGABasePalette(7 + If(int, 8, 0))
            '            }
            'Else
            '    colors = New Color() {
            '                CGABasePalette(cgaColorReg And &HF),
            '                CGABasePalette(2 + If(int, 8, 0)),
            '                CGABasePalette(4 + If(int, 8, 0)),
            '                CGABasePalette(6 + If(int, 8, 0))
            '            }
            'End If

            Select Case VideoMode
                Case VideoModes.Mode4_Graphic_Color_320x200
                    Dim intense As Integer = (cgaColorReg And &H10) >> 1
                    Dim pal1 As Integer = (cgaColorReg >> 5) And (Not (cgaModeReg >> 2)) And 1
                    Dim pal2 As Integer = ((Not cgaColorReg) >> 5) And (Not (cgaModeReg >> 2)) And 1

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
        Dim hRetrace As Boolean = (t Mod ht) <= (ht / 10)
        Dim vRetrace As Boolean = (t Mod vt) <= (vt / 10)

        CGAStatusRegister(CGAStatusRegisters.display_enable) = hRetrace
        CGAStatusRegister(CGAStatusRegisters.vertical_retrace) = vRetrace
    End Sub

    Public MustOverride Sub AutoSize()

    Public Property Zoom As Double
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

    Public Overrides Sub Run()

    End Sub
End Class
