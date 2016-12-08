Imports x8086NetEmu
Imports System.Threading

Public Class FormConsole
    Private mEmulator As x8086

    Private rtfTextStd As String = "{\rtf1\ansi {\colortbl;" +
                            "\red000\green192\blue000;" +
                            "\red192\green192\blue000;" +
                            "\red192\green000\blue192;" +
                            "\red255\green000\blue000;" +
                            "\red255\green000\blue000;" +
                            "}%\par}"
    Private rtfText As String = ""

    Private refreshTimer As New Timer(New TimerCallback(AddressOf UpdateRtf), Nothing, Timeout.Infinite, Timeout.Infinite)

    Private Sub FormConsole_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        SyncLock Me
            RemoveHandler x8086.Output, AddressOf Output
        End SyncLock

        refreshTimer.Dispose()
    End Sub

    Private Sub FormConsole_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        RichTextBoxConsole.Text = ""
    End Sub

    Public Property Emulator As x8086
        Get
            Return mEmulator
        End Get
        Set(value As x8086)
            mEmulator = value

            AddHandler x8086.Output, AddressOf Output
        End Set
    End Property

    Private Sub Output(message As String, reason As x8086.NotificationReasons, arg() As Object)
        rtfText += "\cf1 " + MillTime + ": "

        Select Case reason
            Case x8086.NotificationReasons.Info : rtfText += "\cf2 "
            Case x8086.NotificationReasons.Warn : rtfText += "\cf3 "
            Case x8086.NotificationReasons.Err : rtfText += "\cf4 "
            Case x8086.NotificationReasons.Fck : rtfText += "\cf5 "
        End Select

        rtfText += String.Format(message.Replace("{", "\b {").Replace("}", "}\b0 ") + " \par ", arg)

        refreshTimer.Change(30, Timeout.Infinite)
    End Sub

    Private Sub UpdateRtf()
        Me.Invoke(New MethodInvoker(Sub()
                                        SyncLock Me
                                            RichTextBoxConsole.Rtf = rtfTextStd.Replace("%", rtfText)
                                            RichTextBoxConsole.SelectionStart = RichTextBoxConsole.TextLength
                                            RichTextBoxConsole.ScrollToCaret()
                                        End SyncLock
                                    End Sub))
    End Sub

    Private ReadOnly Property MillTime As String
        Get
            Return String.Format("{0:00}:{1:00}:{2:00}:{3:000}", Now.Hour, Now.Minute, Now.Second, Now.Millisecond)
        End Get
    End Property

    Private Sub CopyToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CopyToolStripMenuItem.Click
        Dim text As String = ""
        If RichTextBoxConsole.SelectedText = "" Then
            text = RichTextBoxConsole.Text
        Else
            text = RichTextBoxConsole.SelectedText
        End If

        Clipboard.SetText(text)
    End Sub

    Private Sub ClearToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ClearToolStripMenuItem.Click
        rtfText = ""
        RichTextBoxConsole.Clear()
    End Sub
End Class