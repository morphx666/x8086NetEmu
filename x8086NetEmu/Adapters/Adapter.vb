Public MustInherit Class Adapter
    Inherits IOPortHandler

    Public Structure XPoint
        Public X As Integer
        Public Y As Integer

        Public Sub New(x As Integer, y As Integer)
            Me.X = x
            Me.Y = y
        End Sub
    End Structure

    Public Structure XSize
        Public Width As Integer
        Public Height As Integer

        Public Sub New(width As Integer, height As Integer)
            Me.Width = width
            Me.Height = height
        End Sub
    End Structure

    Public Structure XRectangle
        Public X As Integer
        Public Y As Integer
        Public Width As Integer
        Public Height As Integer

        Public Sub New(x As Integer, y As Integer, width As Integer, height As Integer)
            Me.X = x
            Me.Y = y
            Me.Width = width
            Me.Height = height
        End Sub

        Public Sub New(x As Integer, y As Integer, size As XSize)
            Me.X = x
            Me.Y = y
            Me.Width = size.Width
            Me.Height = size.Height
        End Sub
    End Structure

    Public MustInherit Class XEventArgs
        Public Enum Keys
            None = &H0
            LButton = &H1
            RButton = &H2
            Cancel = &H3
            MButton = &H4
            XButton1 = &H5
            XButton2 = &H6
            Back = &H8
            Backspace = Back
            Tab = &H9
            LineFeed = &HA
            Clear = &HC
            [Return] = &HD
            Enter = [Return]
            ShiftKey = &H10
            ControlKey = &H11
            Menu = &H12
            Pause = &H13
            Capital = &H14
            CapsLock = &H14
            KanaMode = &H15
            HanguelMode = &H15
            HangulMode = &H15
            JunjaMode = &H17
            FinalMode = &H18
            HanjaMode = &H19
            KanjiMode = &H19
            Escape = &H1B
            IMEConvert = &H1C
            IMENonconvert = &H1D
            IMEAccept = &H1E
            IMEAceept = IMEAccept
            IMEModeChange = &H1F
            Space = &H20
            Prior = &H21
            PageUp = Prior
            [Next] = &H22
            PageDown = [Next]
            [End] = &H23
            Home = &H24
            Left = &H25
            Up = &H26
            Right = &H27
            Down = &H28
            [Select] = &H29
            Print = &H2A
            Execute = &H2B
            Snapshot = &H2C
            PrintScreen = Snapshot
            Insert = &H2D
            Delete = &H2E
            Help = &H2F
            D0 = &H30
            D1 = &H31
            D2 = &H32
            D3 = &H33
            D4 = &H34
            D5 = &H35
            D6 = &H36
            D7 = &H37
            D8 = &H38
            D9 = &H39
            A = &H41
            B = &H42
            C = &H43
            D = &H44
            E = &H45
            F = &H46
            G = &H47
            H = &H48
            I = &H49
            J = &H4A
            K = &H4B
            L = &H4C
            M = &H4D
            N = &H4E
            O = &H4F
            P = &H50
            Q = &H51
            R = &H52
            S = &H53
            T = &H54
            U = &H55
            V = &H56
            W = &H57
            X = &H58
            Y = &H59
            Z = &H5A
            LWin = &H5B
            LeftApplication = LWin
            RWin = &H5C
            RightApplication = RWin
            Apps = &H5D
            Sleep = &H5F
            NumPad0 = &H60
            NumPad1 = &H61
            NumPad2 = &H62
            NumPad3 = &H63
            NumPad4 = &H64
            NumPad5 = &H65
            NumPad6 = &H66
            NumPad7 = &H67
            NumPad8 = &H68
            NumPad9 = &H69
            Keypad0 = &H60
            Keypad1 = &H61
            Keypad2 = &H62
            Keypad3 = &H63
            Keypad4 = &H64
            Keypad5 = &H65
            Keypad6 = &H66
            Keypad7 = &H67
            Keypad8 = &H68
            Keypad9 = &H69
            Multiply = &H6A
            Add = &H6B
            Separator = &H6C
            Subtract = &H6D
            [Decimal] = &H6E
            Divide = &H6F
            Slash = Divide
            F1 = &H70
            F2 = &H71
            F3 = &H72
            F4 = &H73
            F5 = &H74
            F6 = &H75
            F7 = &H76
            F8 = &H77
            F9 = &H78
            F10 = &H79
            F11 = &H7A
            F12 = &H7B
            F13 = &H7C
            F14 = &H7D
            F15 = &H7E
            F16 = &H7F
            F17 = &H80
            F18 = &H81
            F19 = &H82
            F20 = &H83
            F21 = &H84
            F22 = &H85
            F23 = &H86
            F24 = &H87
            NumLock = &H90
            NumberLock = &H90
            Scroll = &H91
            LShiftKey = &HA0
            LeftShift = LShiftKey
            RShiftKey = &HA1
            RightShift = RShiftKey
            LControlKey = &HA2
            LeftControl = LControlKey
            RControlKey = &HA3
            RightControl = RControlKey
            LMenu = &HA4
            LeftAlt = LMenu
            RMenu = &HA5
            RightAlt = RMenu
            BrowserBack = &HA6
            BrowserForward = &HA7
            BrowserRefresh = &HA8
            BrowserStop = &HA9
            BrowserSearch = &HAA
            BrowserFavorites = &HAB
            BrowserHome = &HAC
            VolumeMute = &HAD
            VolumeDown = &HAE
            VolumeUp = &HAF
            MediaNextTrack = &HB0
            MediaPreviousTrack = &HB1
            MediaStop = &HB2
            MediaPlayPause = &HB3
            LaunchMail = &HB4
            SelectMedia = &HB5
            LaunchApplication1 = &HB6
            LaunchApplication2 = &HB7
            OemSemicolon = &HBA
            Semicolon = OemSemicolon
            Oem1 = OemSemicolon
            Oemplus = &HBB
            Equal = Oemplus
            Oemcomma = &HBC
            Comma = Oemcomma
            OemMinus = &HBD
            Minus = OemMinus
            OemPeriod = &HBE
            Period = OemPeriod
            OemQuestion = &HBF
            Oem2 = OemQuestion
            Oemtilde = &HC0
            Oem3 = Oemtilde
            Grave = Oem3
            OemOpenBrackets = &HDB
            LeftBracket = &HDB
            Oem4 = OemOpenBrackets
            OemPipe = &HDC
            Oem5 = OemPipe
            OemCloseBrackets = &HDD
            RightBracket = OemCloseBrackets
            Oem6 = OemCloseBrackets
            OemQuotes = &HDE
            Quote = &HDE
            Oem7 = OemQuotes
            Oem8 = &HDF
            OemBackslash = &HE2
            Backslash = &HE2
            Oem102 = OemBackslash
            ProcessKey = &HE5
            Packet = &HE7
            Attn = &HF6
            Crsel = &HF7
            Exsel = &HF8
            EraseEof = &HF9
            Play = &HFA
            Zoom = &HFB
            NoName = &HFC
            Pa1 = &HFD
            OemClear = &HFE
            Shift = &H10000
            Control = &H20000
            Alt = &H40000
        End Enum

        Public Enum MouseButtons
            Left = 1048576
            Middle = 4194304
            Right = 2097152
        End Enum
    End Class

    Public Class XKeyEventArgs
        Inherits XEventArgs

        Public Alt As Boolean
        Public Control As Boolean
        Public Shift As Boolean
        Public KeyValue As Integer
        Public Handled As Boolean
        Public Modifiers As Integer
        Public SuppressKeyPress As Boolean

        Public Sub New(keyValue As Integer, modifiers As Integer)
            Me.KeyValue = keyValue And (Not modifiers)
            Me.Modifiers = modifiers
            Me.Alt = (modifiers And Keys.Alt) = Keys.Alt
            Me.Control = (modifiers And Keys.Control) = Keys.Control
            Me.Shift = (modifiers And Keys.Shift) = Keys.Shift
        End Sub
    End Class

    Public Class XMouseEventArgs
        Inherits XEventArgs

        Public Button As Integer
        Public X As Integer
        Public Y As Integer

        Public Sub New(button As Integer, x As Integer, y As Integer)
            Me.Button = button
            Me.X = x
            Me.Y = y
        End Sub
    End Class

    Public Structure XColor
        Public A As Byte
        Public R As Byte
        Public G As Byte
        Public B As Byte

        Public Sub New(r As Byte, g As Byte, b As Byte)
            Me.A = 255
            Me.R = r
            Me.G = g
            Me.B = b
        End Sub

        Public Sub New(a As Byte, r As Byte, g As Byte, b As Byte)
            Me.A = a
            Me.R = r
            Me.G = g
            Me.B = b
        End Sub

        Public Shared Function FromArgb(r As Byte, g As Byte, b As Byte) As XColor
            Return New XColor(r, g, b)
        End Function

        Public Shared Function FromArgb(a As Byte, r As Byte, g As Byte, b As Byte) As XColor
            Return New XColor(a, r, g, b)
        End Function

        Public Shared Function FromArgb(v As UInt32) As XColor
            Return New XColor(v >> 24, (v >> 16) And &HFF, (v >> 8) And &HFF, v And &HFF)
        End Function

        Public Function ToArgb() As UInt32
            Return (CUInt(A) << 24) Or (CUInt(R) << 16) Or (CUInt(G) << 8) Or B
        End Function

        Public Overrides Function ToString() As String
            Return $"[A={A}, R={R}, G={G}, B={B}]"
        End Function
    End Structure

    Public Structure XPaintEventArgs
        Public Graphics As Object
        Public ClipRectangle As XRectangle

        Public Sub New(graphics As Object, clipRectangle As XRectangle)
            Me.Graphics = graphics
            Me.ClipRectangle = clipRectangle
        End Sub
    End Structure

    Private mCPU As X8086

    Public Enum AdapterType
        Video
        Keyboard
        Floppy
        IC
        AudioDevice
        Other
        SerialMouseCOM1
        Memory
    End Enum

    Public Sub New()
    End Sub

    Public Sub New(cpu As X8086)
        mCPU = cpu
    End Sub

    Public ReadOnly Property CPU As X8086
        Get
            Return mCPU
        End Get
    End Property

    Public MustOverride Sub InitAdapter()
    Public MustOverride Sub CloseAdapter()
    Public MustOverride ReadOnly Property Type As AdapterType
    Public MustOverride ReadOnly Property Vendor As String
    Public MustOverride ReadOnly Property VersionMajor As Integer
    Public MustOverride ReadOnly Property VersionMinor As Integer
    Public MustOverride ReadOnly Property VersionRevision As Integer

    'Public MustOverride Function [In](port As Integer) As integer Implements IIOPortHandler.In
    'Public MustOverride Sub Out(port As Integer, value As integer) Implements IIOPortHandler.Out
    'Public MustOverride ReadOnly Property Description As String Implements IIOPortHandler.Description
    'Public MustOverride ReadOnly Property Name As String Implements IIOPortHandler.Name

    Public MustOverride Overrides ReadOnly Property Description As String
    Public MustOverride Overrides Function [In](port As UInt16) As Byte
    Public MustOverride Overrides Sub Out(port As UInt16, value As Byte)
    Public MustOverride Overrides ReadOnly Property Name As String

    Public SampleTicks As Long
End Class
