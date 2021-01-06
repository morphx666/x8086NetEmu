Imports System.Threading

Public Class CGAConsole
    Inherits CGAAdapter

    Private blinkCounter As Integer
    Private buffer() As Byte

    Private lastModifiers As ConsoleModifiers

    Private i2a As Image2Ascii
    Private isRendering As Boolean
    Private ratio As New Size(3, 4)
    Private frameRate As Integer = 27

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)
        InitiAdapter()
        AutoSize()

        Console.TreatControlCAsInput = True
        Console.OutputEncoding = New Text.UTF8Encoding()
        Console.Clear()

        i2a = New Image2Ascii() With {
            .Charset = Image2Ascii.Charsets.Standard,
            .ColorMode = Image2Ascii.ColorModes.Color,
            .GrayScaleMode = Image2Ascii.GrayscaleModes.Average,
            .ScanMode = Image2Ascii.ScanModes.Fast
        }

        Tasks.Task.Run(Sub()
                           Do
                               Thread.Sleep(1000 \ frameRate)

                               Try
                                   If MainMode = MainModes.Graphics Then
                                       i2a.ProcessImage(False)

                                       For y As Integer = 0 To Console.WindowHeight - 1
                                           For x As Integer = 0 To Console.WindowWidth - 1
                                               ConsoleCrayon.WriteFast(i2a.Canvas(x)(y).Character,
                                                                        Image2Ascii.ToConsoleColor(i2a.Canvas(x)(y).Color),
                                                                        ConsoleColor.Black,
                                                                        x, y)
                                           Next
                                       Next
                                   End If
                               Catch : End Try
                           Loop Until MyBase.cancelAllThreads
                       End Sub)
    End Sub

    Private Function HasModifier(v As ConsoleModifiers, t As ConsoleModifiers) As Boolean
        Return (v And t) = t
    End Function

    Protected Overrides Sub AutoSize()
        Dim length As Integer = TextResolution.Width * TextResolution.Height * 2
        If buffer Is Nothing OrElse buffer?.Length <> length Then
            ReDim buffer(length - 1)
            ResizeRenderControl()
        End If
    End Sub

    Protected Overrides Sub ResizeRenderControl()
#If Win32 Then
        Select Case MainMode
            Case MainModes.Text
                Console.SetWindowSize(TextResolution.Width, TextResolution.Height)
            Case MainModes.Graphics
                ratio = New Size(Math.Ceiling(GraphicsResolution.Width / Console.LargestWindowWidth),
                                 Math.Ceiling(GraphicsResolution.Height / Console.LargestWindowHeight))
                Console.SetWindowSize(GraphicsResolution.Width / ratio.Width, GraphicsResolution.Height / ratio.Height)
                ResetI2A()
                Console.SetWindowSize(i2a.CanvasSize.Width, i2a.CanvasSize.Height)
        End Select
        Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight)
        Array.Clear(buffer, 0, buffer.Length)
#End If
    End Sub

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
        MyBase.InitVideoMemory(clearScreen)

        Console.Title = "x8086NetEmuConsole - " + VideoMode.ToString()

        If MainMode = MainModes.Graphics Then ResetI2A()

        Select Case Environment.OSVersion.Platform
            Case PlatformID.Win32NT, PlatformID.Win32S, PlatformID.Win32Windows, PlatformID.WinCE
            Case Else
                If mVideoMode <> &HFF Then
                    Dim lw As Integer = -1
                    Dim lh As Integer = -1
                    While Console.WindowWidth <> mTextResolution.Width OrElse Console.WindowHeight <> mTextResolution.Height
                        If lw <> Console.WindowWidth OrElse lh <> Console.WindowHeight Then
                            lw = Console.WindowWidth
                            lh = Console.WindowHeight

                            Console.Clear()
                            ConsoleCrayon.ResetColor()
                            ConsoleCrayon.WriteFast("Unsupported Console Window Size", ConsoleColor.White, ConsoleColor.Red, 0, 0)
                            ConsoleCrayon.ResetColor()
                            Console.WriteLine()
                            Console.WriteLine("The window console cannot be resized on this platform, which will cause the text to be rendered incorrectly")
                            Console.WriteLine()
                            Console.WriteLine($"Expected Resolution for Video Mode {mVideoMode:X2}: {mTextResolution.Width,4} x {mTextResolution.Height,4}")
                            ConsoleCrayon.WriteFast($"Current console window resolution:     {Console.WindowWidth,4} x {Console.WindowHeight,4}", ConsoleColor.White, ConsoleColor.DarkRed, 0, Console.CursorTop)
                            ConsoleCrayon.ResetColor()
                            Console.WriteLine()
                            Console.WriteLine("Manually resize the window to the appropriate resolution or press any key to continue")
                        End If

                        Thread.Sleep(100)
                    End While
                    ConsoleCrayon.ResetColor()
                    Console.Clear()
                End If
        End Select
    End Sub

    Private Sub ResetI2A()
        If i2a IsNot Nothing Then
            If i2a.Bitmap IsNot Nothing Then i2a.Bitmap.Dispose()
            i2a.Bitmap = New DirectBitmap(GraphicsResolution.Width, GraphicsResolution.Height)
            i2a.CanvasSize = New Size(Console.WindowWidth, Console.WindowHeight)
            Console.CursorVisible = False
        End If
    End Sub

    Private Sub HandleKeyPress()
        If Console.KeyAvailable Then
            Dim keyInfo As ConsoleKeyInfo = Console.ReadKey(True)
            Dim keyEvent As New KeyEventArgs(keyInfo.Key)

            HandleModifier(keyInfo.Modifiers, ConsoleModifiers.Shift, Keys.ShiftKey)
            HandleModifier(keyInfo.Modifiers, ConsoleModifiers.Control, Keys.ControlKey)
            HandleModifier(keyInfo.Modifiers, ConsoleModifiers.Alt, Keys.Alt Or Keys.Menu)
            lastModifiers = keyInfo.Modifiers

            MyBase.HandleKeyDown(Me, keyEvent)
            Thread.Sleep(100)
            MyBase.HandleKeyUp(Me, keyEvent)
        End If
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

    Protected Overrides Sub Render()
        If isRendering OrElse CPU Is Nothing Then Exit Sub
        isRendering = True

        If MyBase.VideoEnabled Then
            Try
                Select Case MainMode
                    Case MainModes.Text : RenderText()
                    Case MainModes.Graphics : RenderGraphics()
                End Select
            Catch : End Try
        End If

        HandleKeyPress()

        isRendering = False
    End Sub

    Private Sub RenderGraphics() ' This is cool, I guess, but completely useless...
        Dim b As Byte
        Dim address As UInt32
        Dim xDiv As Integer = If(PixelsPerByte = 4, 2, 3)

        For y As Integer = 0 To GraphicsResolution.Height - 1
            For x As Integer = 0 To GraphicsResolution.Width - 1
                address = mStartGraphicsVideoAddress + ((y >> 1) * 80) + ((y And 1) * &H2000) + (x >> xDiv)
                b = CPU.Memory(address)

                If PixelsPerByte = 4 Then
                    Select Case x And 3
                        Case 3 : b = b And 3
                        Case 2 : b = (b >> 2) And 3
                        Case 1 : b = (b >> 4) And 3
                        Case 0 : b = (b >> 6) And 3
                    End Select
                Else
                    b = (b >> (7 - (x And 7))) And 1
                End If

                i2a.DirectBitmap.Pixel(x, y) = CGAPalette(b)
            Next
        Next
    End Sub

    Private Sub RenderText()
        Dim b0 As Byte
        Dim b1 As Byte

        Dim col As Integer = 0
        Dim row As Integer = 0
        Dim bufIdx As Integer = 0

        Dim cv As Boolean = False

        Dim text As String = ""
        Dim b1c As Byte = CPU.Memory(mStartTextVideoAddress + 1)
        Dim c As Integer
        Dim r As Integer

        Console.CursorVisible = False

        ' The "-4" is to prevent the code from printing the last character and avoid scrolling.
        ' Unfortunately, this causes the last char to not be printed
        For address As Integer = mStartTextVideoAddress To mEndTextVideoAddress + buffer.Length - 4 Step 2
            b0 = CPU.Memory(address)
            b1 = CPU.Memory(address + 1)

            If (blinkCounter <BlinkRate) AndAlso BlinkCharOn AndAlso (b1 And &H80) <> 0 Then b0 = 0

            If b1c <> b1 Then
                ConsoleCrayon.WriteFast(text, b1c.LowNib(), b1c.HighNib(), c, r)
                text = ""
                b1c = b1
                c = col
                r = row
            End If
            text += chars(b0)

            If CursorVisible AndAlso row = CursorRow AndAlso col = CursorCol Then
                cv = blinkCounter <BlinkRate

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

        If text <> "" Then ConsoleCrayon.WriteFast(text, b1c.LowNib(), b1c.HighNib(), c, r)

        If cv Then
            Console.SetCursorPosition(CursorCol, CursorRow)
            Console.CursorVisible = True
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
