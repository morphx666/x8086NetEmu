'
' ConsoleCrayon.cs
'
' Author:
'   Aaron Bockover <abockover@novell.com>
'
' Copyright (C) 2008 Novell, Inc.
'
' Permission is hereby granted, free of charge, to any person obtaining
' a copy of this software and associated documentation files (the
' "Software"), to deal in the Software without restriction, including
' without limitation the rights to use, copy, modify, merge, publish,
' distribute, sublicense, and/or sell copies of the Software, and to
' permit persons to whom the Software is furnished to do so, subject to
' the following conditions:
'
' The above copyright notice and this permission notice shall be
' included in all copies or substantial portions of the Software.
'
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
' EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
' MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
' NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
' LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
' OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
' WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

' Documentation: http://ascii-table.com/ansi-escape-sequences.php

' Modifications and implementation of a simple hyper-text langage by Xavier Flix | 2013
' fc: Fore Color
' bc: Back Color
'
' Example:
' ConsoleCrayon.WriteToConsole("<bc:Red><fc:Gray>Hello World</fc></bc>")

' Console Virtual Terminal Sequences
' https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences

Public NotInheritable Class ConsoleCrayon
#Region "Public API"
    Public Enum TextAlignment
        Left
        Center
        Right
    End Enum

    Public Const ToRadians As Double = Math.PI / 180
    Public Const ToDegrees As Double = 180 / Math.PI

    Public Shared SyncObject As New Object()

    Public Shared Property ConsoleWidth As Integer
    Public Shared Property ConsoleHeight As Integer

    Public Shared Sub WriteFast(text As String, foreColor As ConsoleColor, backColor As ConsoleColor, col As Integer, row As Integer)
        SyncLock SyncObject
            If col < 0 OrElse col >= ConsoleWidth OrElse
               row < 0 OrElse row >= ConsoleHeight Then Exit Sub

            'If ConsoleCrayon.XtermColors Then
            '    Console.Write(ESC + (row + 1).ToString() + ";" + (col + 1).ToString() + "H" +
            '                      GetAnsiColorControlCode(foreColor, True) +
            '                      GetAnsiColorControlCode(backColor, False) +
            '                      text)
            'Else
            If Console.CursorLeft <> col Then Console.CursorLeft = col
            If Console.CursorTop <> row Then Console.CursorTop = row

            If foreColor <> Console.ForegroundColor Then Console.ForegroundColor = foreColor
            If backColor <> Console.BackgroundColor Then Console.BackgroundColor = backColor

            If col + text.Length > ConsoleWidth Then
                Console.Write(text.Substring(0, ConsoleWidth - col))
                WriteFast(text.Substring(ConsoleWidth - col), foreColor, backColor, 0, row + 1)
            Else
                Dim index As Integer = col + row * ConsoleWidth
                Dim size As Integer = ConsoleWidth * ConsoleHeight
                If index + text.Length >= size Then text = text.Substring(0, size - index - 1)

                Console.Write(text)
            End If

            'End If
        End SyncLock
    End Sub

    Public Shared Property ForegroundColor() As ConsoleColor
        Get
            Return Console.ForegroundColor
        End Get
        Set(value As ConsoleColor)
            If Console.ForegroundColor <> value Then SetColor(value, True)
        End Set
    End Property

    Public Shared Property BackgroundColor() As ConsoleColor
        Get
            Return Console.BackgroundColor
        End Get
        Set(value As ConsoleColor)
            If Console.BackgroundColor <> value Then SetColor(value, False)
        End Set
    End Property

    Public Shared Sub RemoveScrollbars()
        Select Case Environment.OSVersion.Platform
            Case PlatformID.Win32NT, PlatformID.Win32S, PlatformID.Win32Windows, PlatformID.WinCE
                Console.BufferWidth = ConsoleWidth
                Console.BufferHeight = ConsoleHeight
        End Select
    End Sub

    Public Shared Sub ResetColor()
        If XtermColors Then
            Console.Write(ColorReset)
        ElseIf Environment.OSVersion.Platform <> PlatformID.Unix AndAlso Not RuntimeIsMono Then
            Console.ResetColor()
        End If
    End Sub

    Private Shared Sub SetColor(color As ConsoleColor, isForeground As Boolean)
        If color < ConsoleColor.Black OrElse color > ConsoleColor.White Then
            Throw New ArgumentOutOfRangeException("color", "Not a ConsoleColor value")
        End If

        If XtermColors Then
            Console.Write(GetAnsiColorControlCode(color, isForeground))
        ElseIf Environment.OSVersion.Platform <> PlatformID.Unix AndAlso Not RuntimeIsMono Then
            If isForeground Then
                Console.ForegroundColor = color
            Else
                Console.BackgroundColor = color
            End If
        End If
    End Sub

    Public Shared Sub WriteToConsole(text As String, Optional addNewLine As Boolean = True)
        Dim textBuffer As String = ""
        Dim tmpText As String

        Dim WriteText = Sub()
                            If textBuffer <> "" Then
                                Console.Write(textBuffer)
                                textBuffer = ""
                            End If
                        End Sub

        For i As Integer = 0 To text.Length - 1
            If i + 4 <= text.Length Then
                tmpText = text.Substring(i, 4)
            Else
                tmpText = text(i)
            End If

            Select Case tmpText
                Case "<fc:"
                    WriteText()
                    i += SetColorFrom(text.Substring(i + 4), True) + 4
                Case "<bc:"
                    WriteText()
                    i += SetColorFrom(text.Substring(i + 4), False) + 4
                Case "/fc>", "/bc>"
                    textBuffer = textBuffer.TrimEnd("<")
                    WriteText()
                    ResetColor()
                    i += 3
                Case Else
                    textBuffer += text(i)
            End Select
        Next

        WriteText()
        If addNewLine Then Console.WriteLine("")
    End Sub

    Public Shared Sub DrawLine(c As Char, fromCol As Integer, fromRow As Integer, toCol As Integer, toRow As Integer, foreColor As ConsoleColor, backColor As ConsoleColor)
        Dim angle As Double = Atan2(toCol - fromCol, toRow - fromRow)
        Dim length As Integer = Math.Sqrt((toCol - fromCol) ^ 2 + (toRow - fromRow) ^ 2)
        Dim px As Integer
        Dim py As Integer
        Dim ca As Double = Math.Cos(angle * ToRadians)
        Dim sa As Double = Math.Sin(angle * ToRadians)

        For radius As Integer = 0 To length
            px = CInt(radius * ca + fromCol)
            py = CInt(radius * sa + fromRow)

            ConsoleCrayon.WriteFast(c, foreColor, backColor, px, py)
        Next
    End Sub

    Private Shared Function SetColorFrom(data As String, isForeground As Boolean) As Integer
        Dim colorName = data.Substring(0, data.IndexOf(">"))
        Dim c As ConsoleColor
        If [Enum].TryParse(Of ConsoleColor)(colorName, c) Then SetColor(c, isForeground)
        Return data.IndexOf(">", colorName.Length)
    End Function

    Private Shared Function Atan2(dx As Double, dy As Double) As Double
        Dim a As Double

        If dy = 0 Then
            a = If(dx > 0, 0, 180)
        Else
            a = Math.Atan(dy / dx) * ToDegrees
            Select Case a
                Case Is > 0
                    If dx < 0 AndAlso dy < 0 Then a += 180
                Case 0
                    If dx < 0 Then a = 180
                Case Is < 0
                    If dy > 0 Then
                        If dx > 0 Then
                            a = Math.Abs(a)
                        Else
                            a += 180
                        End If
                    Else
                        a += 360
                    End If
            End Select
        End If

        Return a
    End Function

    Public Shared Function PadText(text As String, width As Integer, Optional alignment As TextAlignment = TextAlignment.Left) As String
        If text.Length = width Then
            Return text
        ElseIf width < 0 Then
            Return ""
        ElseIf text.Length > width Then
            Return text.Substring(0, width)
        Else
            Select Case alignment
                Case TextAlignment.Left : Return text + New String(" ", width - text.Length)
                Case TextAlignment.Right : Return New String(" ", width - text.Length) + text
                Case TextAlignment.Center : Return String.Format("{0}{1}{0}", New String(" ", (width - text.Length) \ 2), text)
                Case Else : Return text
            End Select
        End If
    End Function
#End Region

#Region "Ansi/VT Code Calculation"
    ' Modified from Mono's System.TermInfoDriver
    ' License: MIT/X11
    ' Authors: Gonzalo Paniagua Javier <gonzalo@ximian.com>
    ' (C) 2005-2006 Novell, Inc <http://www.novell.com>

    Private Shared Function TranslateColor(desired As ConsoleColor, ByRef light As Boolean) As Integer
        light = False
        Select Case desired
            ' Dark colors
            Case ConsoleColor.Black
                Return 0
            Case ConsoleColor.DarkRed
                Return 1
            Case ConsoleColor.DarkGreen
                Return 2
            Case ConsoleColor.DarkYellow
                Return 3
            Case ConsoleColor.DarkBlue
                Return 4
            Case ConsoleColor.DarkMagenta
                Return 5
            Case ConsoleColor.DarkCyan
                Return 6
            Case ConsoleColor.Gray
                Return 7

                ' Light colors
            Case ConsoleColor.DarkGray
                light = True
                Return 0
            Case ConsoleColor.Red
                light = True
                Return 1
            Case ConsoleColor.Green
                light = True
                Return 2
            Case ConsoleColor.Yellow
                light = True
                Return 3
            Case ConsoleColor.Blue
                light = True
                Return 4
            Case ConsoleColor.Magenta
                light = True
                Return 5
            Case ConsoleColor.Cyan
                light = True
                Return 6
            Case Else ' ConsoleColor.White
                light = True
                Return 7
        End Select
    End Function

    Private Const ESC As String = Chr(27) + "["
    Private Const ColorReset As String = ESC + "0m"
    Private Shared Function GetAnsiColorControlCode(color As ConsoleColor, isForeground As Boolean) As String
        ' lighter fg colours are 90 -> 97 rather than 30 -> 37
        ' lighter bg colours are 100 -> 107 rather than 40 -> 47

        Dim light As Boolean
        Dim code As Integer = TranslateColor(color, light) + If(isForeground, 30, 40) + If(light, 60, 0)

        Return ESC + code.ToString() + "m"
    End Function
#End Region

#Region "xterm Detection"
    Private Shared xterm_colors As Boolean? = Nothing

    Public Shared ReadOnly Property XtermColors() As Boolean
        Get
            If xterm_colors Is Nothing Then DetectXtermColors()
            Return xterm_colors.Value
        End Get
    End Property

    <Runtime.InteropServices.DllImport("libc", EntryPoint:="isatty")>
    Private Shared Function _isTty(fd As Integer) As Integer
    End Function

    Private Shared Function IsTty(fd As Integer) As Boolean
        Try
            Return _isTty(fd) = 1
        Catch
            Return False
        End Try
    End Function

    Private Shared Sub DetectXtermColors()
        Dim _xterm_colors As Boolean = False

        Dim term = Environment.GetEnvironmentVariable("TERM")
        If term Is Nothing Then term = ""
        If term.StartsWith("xterm") Then term = "xterm"

        Select Case term
            Case "xterm", "rxvt", "rxvt-unicode"
                'If Environment.GetEnvironmentVariable("COLORTERM") IsNot Nothing Then
                _xterm_colors = True
                'End If
            Case "xterm-color"
                _xterm_colors = True
            Case "linux"
                _xterm_colors = True
        End Select

        xterm_colors = _xterm_colors AndAlso IsTty(1) AndAlso IsTty(2)
    End Sub

#End Region

#Region "Runtime Detection"
    Private Shared runtime_is_mono As Boolean?
    Public Shared ReadOnly Property RuntimeIsMono() As Boolean
        Get
            If Not runtime_is_mono.HasValue Then
                runtime_is_mono = Type.[GetType]("System.MonoType") IsNot Nothing
            End If
            Return runtime_is_mono.Value
        End Get
    End Property
#End Region

#Region "Tests"
    Public Shared Sub Test()
        TestSelf()
        Console.WriteLine()
        TestAnsi()
        Console.WriteLine()
        TestRuntime()
    End Sub

    Private Shared Sub TestSelf()
        Console.WriteLine("==SELF TEST==")
        For Each color As ConsoleColor In [Enum].GetValues(GetType(ConsoleColor))
            ForegroundColor = color
            Console.Write(color)
            ResetColor()
            Console.Write(" :: ")
            BackgroundColor = color
            Console.Write(color)
            ResetColor()
            Console.WriteLine()
        Next
    End Sub

    Private Shared Sub TestAnsi()
        Console.WriteLine("==ANSI TEST==")
        For Each color As ConsoleColor In [Enum].GetValues(GetType(ConsoleColor))
            Dim color_code_fg As String = GetAnsiColorControlCode(color, True)
            Dim color_code_bg As String = GetAnsiColorControlCode(color, False)
            Console.Write("{0}{1}: {2}{3} :: {4}{1}: {5}{3}", color_code_fg,
                                                              color,
                                                              color_code_fg.Substring(2),
                                                              ColorReset,
                                                              color_code_bg,
                                                              color_code_bg.Substring(2))
            Console.WriteLine()
        Next
    End Sub

    Private Shared Sub TestRuntime()
        Console.WriteLine("==RUNTIME TEST==")
        For Each color As ConsoleColor In [Enum].GetValues(GetType(ConsoleColor))
            Console.ForegroundColor = color
            Console.Write(color)
            Console.ResetColor()
            Console.Write(" :: ")
            Console.BackgroundColor = color
            Console.Write(color)
            Console.ResetColor()
            Console.WriteLine()
        Next
    End Sub
#End Region
End Class