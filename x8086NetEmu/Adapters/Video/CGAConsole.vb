Imports System.Threading

Public Class CGAConsole
    Inherits CGAAdapter

    Private loopThread As Thread
    Private waiter As AutoResetEvent

    Private buffer() As Byte

    Private lastModifiers As ConsoleModifiers

    Public Sub New(cpu As x8086)
        MyBase.New(cpu)
        InitiAdapter()

        Console.TreatControlCAsInput = True
        Console.OutputEncoding = New System.Text.UTF8Encoding()
        Console.Clear()

        waiter = New AutoResetEvent(False)
        loopThread = New Thread(AddressOf MainLoop)
        loopThread.Start()
    End Sub

    Private Sub MainLoop()
        Do
            waiter.WaitOne(1000 / (VERTSYNC / 8))

            If Console.KeyAvailable Then
                Dim keyInfo = Console.ReadKey(True)
                Dim keyEvent As New KeyEventArgs(keyInfo.Key)

                HandleModifier(keyInfo.Modifiers, ConsoleModifiers.Shift, Keys.ShiftKey)
                HandleModifier(keyInfo.Modifiers, ConsoleModifiers.Control, Keys.ControlKey)
                HandleModifier(keyInfo.Modifiers, ConsoleModifiers.Alt, Keys.Alt)
                lastModifiers = keyInfo.Modifiers

                OnKeyDown(Me, keyEvent)
                Thread.Sleep(100)
                OnKeyUp(Me, keyEvent)
            End If
        Loop Until cancelAllThreads
    End Sub

    Private Sub HandleModifier(v As ConsoleModifiers, t As ConsoleModifiers, k As Keys)
        If HasModifier(v, t) AndAlso Not HasModifier(lastModifiers, t) Then
            OnKeyDown(Me, New KeyEventArgs(k))
            Thread.Sleep(100)
        ElseIf Not HasModifier(v, t) AndAlso HasModifier(lastModifiers, t) Then
            OnKeyUp(Me, New KeyEventArgs(k))
            Thread.Sleep(100)
        End If
    End Sub

    Private Function HasModifier(v As ConsoleModifiers, t As ConsoleModifiers) As Boolean
        Return (v And t) = t
    End Function

    Public Overrides Sub AutoSize()
        SyncLock lockObject
            'Dim length = Console.WindowWidth * Console.WindowHeight * 2
            Dim length = TextResolution.Width * TextResolution.Height * 2
            If buffer Is Nothing OrElse buffer.Length <> length Then ReDim buffer(length - 1)

            ResizeRenderControl()
        End SyncLock
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

    Protected Overrides Sub Render()
        Dim b0 As Byte
        Dim b1 As Byte

        Dim col As Integer = 0
        Dim row As Integer = 0
        Dim bufIdx As Integer = 0

        ' The "-4" is to prevent the code from printing the last character and avoid scrolling.
        ' Unfortunately, this causes the last char to not be printed
        For address As Integer = StartTextVideoAddress To StartTextVideoAddress + buffer.Length - 4 Step 2
            b0 = CPU.RAM(address)
            b1 = CPU.RAM(address + 1)

            If buffer(bufIdx) <> b0 OrElse buffer(bufIdx + 1) <> b1 Then
                ConsoleCrayon.WriteFast(chars(b0), b1.LowNib(), b1.HighNib(), col, row)

                buffer(bufIdx) = b0
                buffer(bufIdx + 1) = b1
            End If

            col += 1
            If col = TextResolution.Width Then
                col = 0
                row += 1
                If row = TextResolution.Height Then Exit For
            End If

            bufIdx += 2
        Next

        If CursorVisible Then Console.SetCursorPosition(CursorCol, CursorRow)
    End Sub

    Public Overrides Sub Run()
    End Sub

    Protected Overrides Sub OnDataRegisterChanged()
        MyBase.OnDataRegisterChanged()
        Console.CursorVisible = CursorVisible
    End Sub
End Class
