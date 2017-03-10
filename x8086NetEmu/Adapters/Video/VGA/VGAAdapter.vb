Public MustInherit Class VGAAdapter
    Inherits VideoAdapter

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

    Public Enum MainModes
        Unknown = -1
        Text = 0
        Graphics = 2
    End Enum

    Public MustOverride Overrides Sub AutoSize()
    Protected MustOverride Sub Render()

    Private VGA_SC(&H100 - 1) As Byte
    Private VGA_CRTC(&H100 - 1) As Byte
    Private VGA_ATTR(&H100 - 1) As Byte
    Private VGA_GC(&H100 - 1) As Byte
    Private flip3C0 As Boolean
    Private latchRGB = 0
    Private latchPal = 0
    Private VGA_latch(4 - 1) As Byte
    Private stateDAC = 0
    Private latchReadRGB = 0
    Private latchReadPal = 0
    Private RAM(&H3DF - &H3C0 - 1) As Byte
    Private tempRGB As Integer
    Private palettevga(256 - 1) As Integer
    Private curX As Integer
    Private curY As Integer
    Private cols As Integer = 80
    Private rows As Integer = 25
    Private vgapage As Integer
    Private curPos As Integer
    Private curVisible As Integer
    Private vtotal As Integer
    Private port3DA As Integer

    Protected lockObject As New Object()

    Private mCPU As x8086
    Private mMainMode As MainModes

    Public Sub New(cpu As x8086)
        mCPU = cpu

        'ValidPortAddress.Clear()
        For i As Integer = &H3C0 To &H3DF
            ValidPortAddress.Add(i)
        Next

        For i As Integer = 0 To VGABasePalette.Length - 1
            palettevga(i) = VGABasePalette(i).ToArgb()
        Next
    End Sub

    Public Overrides Sub Run()

    End Sub

    Public Overrides Sub CloseAdapter()
        'isInit = False
        'cancelAllThreads = True

        Application.DoEvents()
    End Sub

    Public ReadOnly Property MainMode As MainModes
        Get
            Return mMainMode
        End Get
    End Property

    Public Sub HandleKeyDown(sender As Object, e As KeyEventArgs)
        MyBase.OnKeyDown(Me, e)
        If e.Handled Then Exit Sub
        'Debug.WriteLine("KEY DOWN: " + e.KeyCode.ToString() + " | " + e.Modifiers.ToString())
        If mCPU.Keyboard IsNot Nothing Then mCPU.Sched.HandleInput(New ExternalInputEvent(mCPU.Keyboard, e, False))
        e.Handled = True
    End Sub

    Public Sub HandleKeyUp(sender As Object, e As KeyEventArgs)
        MyBase.OnKeyUp(Me, e)
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

    Public Overrides Function [In](port As Integer) As Integer
        Select Case port
            Case &H3C1
                Return VGA_ATTR(RAM(&H3C0 - &H3C0))

            Case &H3C5
                Return VGA_ATTR(RAM(&H3C4 - &H3C0))

            Case &H3D5
                Return VGA_ATTR(RAM(&H3D4 - &H3C0))

            Case &H3C7
                Return stateDAC

            Case &H3C8
                Return latchReadPal

            Case &H3C9
                Select Case latchReadRGB
                    Case 0 ' R
                        Return (palettevga(latchReadPal) >> 2) And 63
                    Case 1 ' G
                        Return (palettevga(latchReadPal) >> 10) And 63
                    Case 2 ' B
                        latchReadRGB = 0
                        Dim b As Integer = (palettevga(latchReadPal) >> 18) And 63
                        latchReadPal += 1
                        Return b
                End Select

            Case &H3DA
                Return port3DA

        End Select

        Return RAM(port - &H3C0)
    End Function

    Public Overrides Sub Out(port As Integer, value As Integer)
        Dim ramAddr As Integer = port - &H3C0
        value = value And &HFF

        Select Case port
            Case &H3C0
                If flip3C0 Then
                    flip3C0 = False
                    RAM(ramAddr) = value
                Else
                    flip3C0 = True
                    VGA_ATTR(ramAddr) = value
                End If

            Case &H3C4
                RAM(ramAddr) = value

            Case &H3C5
                VGA_SC(RAM(ramAddr)) = value

            Case &H3D4
                RAM(ramAddr) = value

            Case &H3C7
                latchReadPal = value
                latchReadRGB = 0
                stateDAC = 0

            Case &H3C8
                latchPal = value
                latchRGB = 0
                tempRGB = 0
                stateDAC = 3

            Case &H3C9
                value = value And 63
                Select Case latchRGB
                    Case 0 ' R
                        tempRGB = value << 2
                    Case 1 ' G
                        tempRGB = tempRGB Or (value << 10)
                    Case 2 ' B
                        tempRGB = tempRGB Or (value << 18)
                        palettevga(latchPal) = tempRGB
                        latchPal += 1
                End Select
                latchRGB = (latchRGB + 1) Mod 3

            Case &H3D5
                VGA_CRTC(RAM(&H3D4 - &H3C0)) = value
                If RAM(&H3D4 - &H3C0) = &HE Then
                    curPos = (curPos And &HFF) Or (value << 8)
                ElseIf RAM(&H3D4 - &H3C0) = &HF Then
                    curPos = (curPos And &HFF00) Or value
                End If

                curY = curPos / cols
                curX = curPos Mod cols

                If RAM(&H3D4 - &H3C0) = &H6 Then
                    vtotal = value Or ((VGA_GC(7) And 1) << 8) Or (If(VGA_GC(7) And 32 <> 0, 1, 0) << 9)
                End If

            Case &H3CF
                VGA_GC(RAM(&H3CE - &H3C0)) = value

            Case Else
                RAM(ramAddr) = value

        End Select
    End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return "VGA"
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "VGA Emulator"
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
End Class
