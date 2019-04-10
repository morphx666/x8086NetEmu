Imports x8086NetEmu
Imports System.Threading

Public Class FormConsole
#If Win32 Then
    <Runtime.InteropServices.DllImport("user32.dll")>
    Public Shared Function LockWindowUpdate(hWndLock As IntPtr) As Boolean
    End Function
#End If

    Private mEmulator As X8086

    Private Const rtfTextStd As String = "{\rtf1\ansi {\colortbl;" +
                                          "\red000\green192\blue000;" +
                                          "\red192\green192\blue000;" +
                                          "\red192\green000\blue192;" +
                                          "\red255\green000\blue000;" +
                                          "\red255\green000\blue000;" +
                                          "\red080\green080\blue255;" +
                                          "}%\par}"
    Private rtfText As String = ""
    Private lastMesssage As String = ""
    Private repeatCount As Integer = 0
    Private lastArg() As Object = {""}
    Private ReadOnly refreshTimer As New Timer(New TimerCallback(AddressOf UpdateRtf), Nothing, Timeout.Infinite, Timeout.Infinite)

    Private Sub FormConsole_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        SyncLock Me
            RemoveHandler X8086.Output, AddressOf Output
        End SyncLock

        refreshTimer.Dispose()
    End Sub

    Private Sub FormConsole_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        RichTextBoxConsole.Text = ""

        AddHandler CopyToolStripMenuItem.Click, Sub()
                                                    If RichTextBoxConsole.SelectedText = "" Then
                                                        Clipboard.SetText(RichTextBoxConsole.Text)
                                                    Else
                                                        Clipboard.SetText(RichTextBoxConsole.SelectedText)
                                                    End If
                                                End Sub
        AddHandler ClearToolStripMenuItem.Click, Sub()
                                                     rtfText = ""
                                                     RichTextBoxConsole.Clear()
                                                 End Sub
    End Sub

    Public Property Emulator As X8086
        Get
            Return mEmulator
        End Get
        Set(value As X8086)
            mEmulator = value
            AddHandler X8086.Output, AddressOf Output
        End Set
    End Property

    Private Sub Output(message As String, reason As X8086.NotificationReasons, arg() As Object)
        If lastMesssage = message AndAlso HasSameArguments(arg) Then
            repeatCount += 1
            Exit Sub
        End If
        lastMesssage = message
        lastArg = arg

        If repeatCount > 0 Then
            SendOutput($"^^^ Last message repeated {repeatCount} time{If(repeatCount > 1, "s", "")}", X8086.NotificationReasons.Dbg, arg)
            repeatCount = 0
        End If
        SendOutput(message, reason, arg)
    End Sub

    Private Function HasSameArguments(arg() As Object) As Boolean
        If arg.Length <> lastArg.Length Then Return False
        If arg.Length = 0 Then Return True

        For i As Integer = 0 To arg.Length - 1
            If arg(i) <> lastArg(i) Then Return False
        Next
        Return True
    End Function

    Private Sub SendOutput(message As String, reason As X8086.NotificationReasons, arg() As Object)
        message = message.Replace("\", "\\")
        rtfText += "\cf1 " + MillTime + ": "

        Select Case reason
            Case X8086.NotificationReasons.Info : rtfText += "\cf2 "
            Case X8086.NotificationReasons.Warn : rtfText += "\cf3 "
            Case X8086.NotificationReasons.Err : rtfText += "\cf4 "
            Case X8086.NotificationReasons.Fck : rtfText += "\cf5 "
            Case X8086.NotificationReasons.Dbg : rtfText += "\cf6 "
        End Select

        rtfText += String.Format(message.Replace("{", "\b {").Replace("}", "}\b0 ") + " \par ", arg)

        refreshTimer.Change(250, Timeout.Infinite)
    End Sub

    Private Sub UpdateRtf()
        Me.BeginInvoke(Sub()
                           SyncLock Me
#If Win32 Then
                               LockWindowUpdate(RichTextBoxConsole.Handle)
#End If
                               RichTextBoxConsole.Rtf = rtfTextStd.Replace("%", rtfText)
                               RichTextBoxConsole.SelectionStart = RichTextBoxConsole.TextLength
                               RichTextBoxConsole.ScrollToCaret()
#If Win32 Then
                               LockWindowUpdate(0)
#End If
                           End SyncLock
                       End Sub)
    End Sub

    Private ReadOnly Property MillTime As String
        Get
            Return String.Format("{0:00}:{1:00}:{2:00}:{3:000}", Now.Hour, Now.Minute, Now.Second, Now.Millisecond)
        End Get
    End Property
End Class