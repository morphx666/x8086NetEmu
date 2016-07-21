Imports SdlDotNet.Core
Imports SdlDotNet.Graphics
Imports SdlDotNet.Graphics.Primitives
Imports System.Drawing
Imports System.Threading

' MODE 0x13: http://www.brackeen.com/vga/basics.html

Public Class CGASDL
    Inherits CGAAdapter

    Private screen As Surface
    Private font As SdlDotNet.Graphics.Font
    Private fontSize As Size

    Private mCPU As x8086

    Private colorsCache(16 - 1) As SolidBrush

    Private lastModifiers As ConsoleModifiers

    Public Sub New(cpu As x8086, renderControl As Control)
        MyBase.New(cpu, False)
        mCPU = cpu

        For i As Integer = 0 To 255
            If i >= 32 AndAlso i < 255 Then
                chars(i) = Chr(i)
            Else
                chars(i) = " "
            End If
        Next

        InitiAdapter()
    End Sub

    Public Overrides Sub InitiAdapter()
        MyBase.InitiAdapter()

        AddHandler Events.Tick, AddressOf Render
        AddHandler Events.KeyboardUp, Sub(s As Object, e As SdlDotNet.Input.KeyboardEventArgs) OnKeyUp(Me, New KeyEventArgs(e.Key))
        AddHandler Events.KeyboardDown, Sub(s As Object, e As SdlDotNet.Input.KeyboardEventArgs) OnKeyDown(Me, New KeyEventArgs(e.Key))
    End Sub

    Public Overrides Sub AutoSize()
        ResizeRenderControl()
    End Sub

    Private Overloads Sub ResizeRenderControl()
        SyncLock lockObject
            If screen Is Nothing Then
                font = New SdlDotNet.Graphics.Font("c:\windows\fonts\Perfect DOS VGA 437.ttf", 16)
                fontSize = font.SizeText("X")
            Else
                screen.Close()
            End If

            Select Case MainMode
                Case MainModes.Text
                    screen = Video.SetVideoMode(MyBase.TextResolution.Width * fontSize.Width, MyBase.TextResolution.Height * fontSize.Height, False)
                Case MainModes.Graphics
                    screen = Video.SetVideoMode(MyBase.VideoResolution.Width, MyBase.VideoResolution.Height, False)
            End Select

            Video.WindowCaption = "x8086NetEmu SDL"

            screen.Fill(New Rectangle(New Point(0, 0), screen.Size), Color.Black)
        End SyncLock
    End Sub

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
        MyBase.InitVideoMemory(clearScreen)
        ResizeRenderControl()
    End Sub

    Protected Overrides Sub Render()
        'SyncLock lockObject
        Select Case MainMode
            Case MainModes.Text : RenderText()
            Case MainModes.Graphics : RenderGraphics()
        End Select
        'End SyncLock
    End Sub

    Private Sub RenderText()
        Dim b0 As Byte
        Dim b1 As Byte

        Dim col As Integer = 0
        Dim row As Integer = 0

        Dim p As New Point(0, 0)

        'g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit

        For address As Integer = StartTextVideoAddress To EndTextVideoAddress Step 2
            b0 = mCPU.RAM(address)
            b1 = mCPU.RAM(address + 1)

            Dim text = font.Render(chars(b0), CGAPalette(b1.LowNib()), CGAPalette(b1.HighNib()))
            screen.Blit(text, p)

            'If CursorVisible AndAlso row = CursorRow AndAlso col = CursorCol Then
            '    If (Not BlinkCursor) OrElse (blinkCounter < 10) Then
            '        g.FillRectangle(cursorBrush, r.X + 1, r.Y, cursorSize.Width, cursorSize.Height - 2)
            '    End If
            '    blinkCounter = (blinkCounter + 1) Mod 20
            'End If

            p.X += fontSize.Width
            col += 1
            If col = TextResolution.Width Then
                col = 0
                row += 1
                If row = TextResolution.Height Then Exit For

                p.X = 0
                p.Y += fontSize.Height
            End If
        Next

        screen.Update()
    End Sub

    Private Sub RenderGraphics()

    End Sub

    Public Overrides Sub Run()
    End Sub

    Protected Overrides Sub OnDataRegisterChanged()
        MyBase.OnDataRegisterChanged()
        Console.CursorVisible = CursorVisible
    End Sub
End Class
