Imports System.Threading

Public Class CGAConsole
    Inherits CGAAdapter

    Private waiter As AutoResetEvent

    Private blinkCounter As Integer

    Private buffer() As Byte

    Private lastModifiers As ConsoleModifiers

    Public Sub New(cpu As x8086)
        MyBase.New(cpu)
        InitiAdapter()

        Console.TreatControlCAsInput = True
        Console.OutputEncoding = New Text.UTF8Encoding()
        Console.Clear()
    End Sub

    Private Sub HandleModifier(v As ConsoleModifiers, t As ConsoleModifiers, k As Keys)
        If HasModifier(v, t) AndAlso Not HasModifier(lastModifiers, t) Then
            MyBase.HandleKeyDown(Me, New KeyEventArgs(k))
            Thread.Sleep(100)
        ElseIf Not HasModifier(v, t) AndAlso HasModifier(lastModifiers, t) Then
            MyBase.HandleKeyUp(Me, New KeyEventArgs(k))
            Thread.Sleep(100)
        End If
    End Sub

    Private Function HasModifier(v As ConsoleModifiers, t As ConsoleModifiers) As Boolean
        Return (v And t) = t
    End Function

    Public Overrides Sub AutoSize()
        'Dim length = Console.WindowWidth * Console.WindowHeight * 2
        Dim length = TextResolution.Width * TextResolution.Height * 2
        If buffer Is Nothing OrElse buffer.Length <> length Then ReDim buffer(length - 1)

        ResizeRenderControl()
    End Sub

    Private Overloads Sub ResizeRenderControl()
#If Win32 Then
        Console.SetWindowSize(TextResolution.Width, TextResolution.Height)
        Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight)
#End If
    End Sub

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
        MyBase.InitVideoMemory(clearScreen)
        Console.Title = "x8086 Emu - " + VideoMode.ToString()
    End Sub

    Private isBusy As Boolean

    Private Sub HandleKeyPress()
        If Console.KeyAvailable Then
            Dim keyInfo As ConsoleKeyInfo = Console.ReadKey(True)
            Dim keyEvent As New KeyEventArgs(keyInfo.Key)

            HandleModifier(keyInfo.Modifiers, ConsoleModifiers.Shift, Keys.ShiftKey)
            HandleModifier(keyInfo.Modifiers, ConsoleModifiers.Control, Keys.ControlKey)
            HandleModifier(keyInfo.Modifiers, ConsoleModifiers.Alt, Keys.Alt)
            lastModifiers = keyInfo.Modifiers

            MyBase.HandleKeyDown(Me, keyEvent)
            Thread.Sleep(100)
            MyBase.HandleKeyUp(Me, keyEvent)
        End If
    End Sub

    Protected Overrides Sub Render()
        Dim b0 As Byte
        Dim b1 As Byte

        Dim col As Integer = 0
        Dim row As Integer = 0
        Dim bufIdx As Integer = 0

        Dim cv As Boolean = False

        HandleKeyPress()

        ' The "-4" is to prevent the code from printing the last character and avoid scrolling.
        ' Unfortunately, this causes the last char to not be printed
        For address As Integer = StartTextVideoAddress To StartTextVideoAddress + buffer.Length - 4 Step 2
            b0 = CPU.RAM(address)
            b1 = CPU.RAM(address + 1)

            If (blinkCounter < BlinkRate) AndAlso BlinkCharOn AndAlso (b1 And &H80) Then b0 = 0

            If buffer(bufIdx) <> b0 OrElse buffer(bufIdx + 1) <> b1 Then
                ConsoleCrayon.WriteFast(chars(b0), b1.LowNib(), b1.HighNib(), col, row)
                buffer(bufIdx) = b0
                buffer(bufIdx + 1) = b1
            End If

            If CursorVisible AndAlso row = CursorRow AndAlso col = CursorCol Then
                If blinkCounter < BlinkRate Then cv = True

                If blinkCounter >= 2 * BlinkRate Then
                    blinkCounter = 0
                Else
                    blinkCounter += 1
                End If
            End If

            col += 1
            If col = TextResolution.Width Then
                col = 0
                row += 1
                If row = TextResolution.Height Then Exit For
            End If

            bufIdx += 2
        Next

        If cv Then
            Console.SetCursorPosition(CursorCol, CursorRow)
            Console.CursorVisible = True
        Else
            Console.CursorVisible = False
        End If
    End Sub

    Public Overrides Sub Run()
    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "CGA Console Adapter"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "CGA Console"
        End Get
    End Property
End Class
