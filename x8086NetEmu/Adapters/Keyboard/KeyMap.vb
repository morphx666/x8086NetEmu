Imports x8086NetEmu.Adapter.XEventArgs

Public Class KeyMap
    ' Enables sending virtual shift up/down events to deal with non-XT keys.
    Private useVirtualShift As Boolean = True

    ' Map Java e.KeyCode code to PC scan code and extra flags.
    Private keytbl() As Integer

    ' Must send E0 escape with this scancode
    Private Const KEY_EXTEND = &H100

    ' Need negative shift/numlock state
    Private Const KEY_NONUM = &H200

    ' Need negative shift state
    Private Const KEY_NOSHIFT = &H400

    ' Could be numpad or edit block depending on e.KeyCode location
    Private Const KEY_EDIT = &H800

    ' Scan code of left Shift e.KeyCode
    Public Const SCAN_LSHIFT = 42

    ' Scan code of right Shift e.KeyCode
    Public Const SCAN_RSHIFT = 54

    ' Scan code of Ctrl e.KeyCode
    Public Const SCAN_CTRL = 29

    ' Scan code of Alt e.KeyCode
    Public Const SCAN_ALT = 56

    ' True if we inverted the shift state by sending a virtual shift code.
    Private virtualShiftState As Boolean

    ' True if PrintScreen is down in its SysRq role.
    Private isSysRq As Boolean

    ' Mask of state keys that we believe to be physically down.
    Private Const MASK_LSHIFT = 1
    Private Const MASK_RSHIFT = 2
    Private Const MASK_LALT = 4
    Private Const MASK_RALT = 8
    Private Const MASK_LCTRL = 16
    Private Const MASK_RCTRL = 32
    Private Const MASK_NUMLOCK = 64
    Private stateKeyMask As Integer

    Public Sub New()
        virtualShiftState = False
        isSysRq = False
        stateKeyMask = 0

        ReDim keytbl(256 - 1)
        keytbl(Keys.Escape) = 1
        keytbl(Keys.D1) = 2
        keytbl(Keys.D2) = 3
        keytbl(Keys.D3) = 4
        keytbl(Keys.D4) = 5
        keytbl(Keys.D5) = 6
        keytbl(Keys.D6) = 7
        keytbl(Keys.D7) = 8
        keytbl(Keys.D8) = 9
        keytbl(Keys.D9) = 10
        keytbl(Keys.D0) = 11
        keytbl(Keys.OemMinus) = 12
        keytbl(Keys.Oemplus) = 13
        keytbl(Keys.Back) = 14
        keytbl(Keys.Tab) = 15
        keytbl(Keys.Q) = 16
        keytbl(Keys.W) = 17
        keytbl(Keys.E) = 18
        keytbl(Keys.R) = 19
        keytbl(Keys.T) = 20
        keytbl(Keys.Y) = 21
        keytbl(Keys.U) = 22
        keytbl(Keys.I) = 23
        keytbl(Keys.O) = 24
        keytbl(Keys.P) = 25
        keytbl(Keys.OemOpenBrackets) = 26
        keytbl(Keys.OemCloseBrackets) = 27
        keytbl(Keys.Enter) = 28
        keytbl(Keys.ControlKey) = 29
        keytbl(Keys.A) = 30
        keytbl(Keys.S) = 31
        keytbl(Keys.D) = 32
        keytbl(Keys.F) = 33
        keytbl(Keys.G) = 34
        keytbl(Keys.H) = 35
        keytbl(Keys.J) = 36
        keytbl(Keys.K) = 37
        keytbl(Keys.L) = 38
        keytbl(Keys.OemSemicolon) = 39
        keytbl(Keys.OemQuotes) = 40
        keytbl(Keys.Oemtilde) = 41
        keytbl(Keys.ShiftKey) = SCAN_LSHIFT
        keytbl(Keys.OemPipe) = 43 ' Keys.OemBackslash
        keytbl(Keys.Z) = 44
        keytbl(Keys.X) = 45
        keytbl(Keys.C) = 46
        keytbl(Keys.V) = 47
        keytbl(Keys.B) = 48
        keytbl(Keys.N) = 49
        keytbl(Keys.M) = 50
        keytbl(Keys.Oemcomma) = 51
        keytbl(Keys.OemPeriod) = 52
        keytbl(Keys.OemQuestion) = 53
        keytbl(Keys.Divide) = 53 Or KEY_EXTEND
        keytbl(Keys.Multiply) = 55
        keytbl(18) = 56 ' ALT
        keytbl(Keys.Space) = 57
        keytbl(Keys.CapsLock) = 58
        keytbl(Keys.F1) = 59
        keytbl(Keys.F2) = 60
        keytbl(Keys.F3) = 61
        keytbl(Keys.F4) = 62
        keytbl(Keys.F5) = 63
        keytbl(Keys.F6) = 64
        keytbl(Keys.F7) = 65
        keytbl(Keys.F8) = 66
        keytbl(Keys.F9) = 67
        keytbl(Keys.F10) = 68
        keytbl(Keys.NumLock) = 69
        keytbl(Keys.Scroll) = 70
        keytbl(Keys.NumPad7) = 71
        keytbl(Keys.Home) = 71 Or KEY_EDIT Or KEY_NOSHIFT
        keytbl(Keys.NumPad8) = 72
        keytbl(Keys.Up) = 72 Or KEY_EXTEND Or KEY_NONUM
        keytbl(Keys.NumPad9) = 73
        keytbl(Keys.PageUp) = 73 Or KEY_EDIT
        keytbl(Keys.Subtract) = 74
        keytbl(Keys.NumPad4) = 75
        keytbl(Keys.Left) = 75 Or KEY_EXTEND Or KEY_NONUM
        keytbl(Keys.NumPad5) = 76
        keytbl(Keys.NumPad6) = 77
        keytbl(Keys.Right) = 77 Or KEY_EXTEND Or KEY_NONUM
        keytbl(Keys.Add) = 78
        keytbl(Keys.NumPad1) = 79
        keytbl(Keys.End) = 79 Or KEY_EDIT
        keytbl(Keys.NumPad2) = 80
        keytbl(Keys.Down) = 80 Or KEY_EXTEND Or KEY_NONUM
        keytbl(Keys.NumPad3) = 81
        keytbl(Keys.PageDown) = 81 Or KEY_EDIT
        keytbl(Keys.NumPad0) = 82
        keytbl(Keys.Insert) = 82 Or KEY_EDIT
        keytbl(Keys.Decimal) = 83
        keytbl(Keys.Delete) = 83 Or KEY_EDIT
        keytbl(Keys.PrintScreen) = 84
        keytbl(Keys.F11) = 87
        keytbl(Keys.F12) = 88
    End Sub

    Public Function GetScanCode(keyValue As Integer) As Integer
        Return keytbl(keyValue And &HFF)
    End Function
End Class