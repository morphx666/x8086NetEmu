Imports x8086NetEmu

Public Class FloppyImgCtrl
    Private mIndex As Integer
    Private mEmulator As x8086

    Public Property Emulator As x8086
        Get
            Return mEmulator
        End Get
        Set(value As x8086)
            mEmulator = value

            UpdateUI()
        End Set
    End Property

    Public Property Index As Integer
        Get
            Return mIndex
        End Get
        Set(value As Integer)
            mIndex = value

            UpdateUI()
        End Set
    End Property

    Private Sub ButtonLoad_Click(sender As Object, e As EventArgs) Handles ButtonLoad.Click
        Using dlg As New OpenFileDialog()
            dlg.Title = "Select Floppy Disk Image"
            dlg.Filter = "Supported Floppy Disk Images|*.ima;*.img;*.vfd|All Files|*.*"
            If dlg.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
                Dim ro As Boolean = MsgBox("Would you like to mount this image in Read Only mode?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes
                TextBoxImageFileName.Text = dlg.FileName

                Try
                    mEmulator.FloppyContoller.DiskImage(mIndex) = New DiskImage(dlg.FileName, ro)
                Catch ex As Exception
                    MsgBox("Unable to mount image: " + ex.Message, MsgBoxStyle.Critical)
                    Eject()
                End Try

                UpdateUI()
            End If
        End Using
    End Sub

    Private Sub ButtonEject_Click(sender As Object, e As EventArgs) Handles ButtonEject.Click
        Eject()
    End Sub

    Private Sub Eject()
        If mEmulator.FloppyContoller.DiskImage(mIndex) IsNot Nothing Then
            mEmulator.FloppyContoller.DiskImage(mIndex).Close()
            mEmulator.FloppyContoller.DiskImage(mIndex) = Nothing
        End If

        UpdateUI()
    End Sub

    Private Sub UpdateUI()
        If mEmulator Is Nothing Then Exit Sub

        Label1.Text = "Floppy " + Chr(65 + mIndex) + ":"

        If mEmulator.FloppyContoller.DiskImage(mIndex) Is Nothing Then
            TextBoxImageFileName.Text = ""

            ButtonEject.Enabled = False
            ButtonLoad.Enabled = True
        Else
            TextBoxImageFileName.Text = mEmulator.FloppyContoller.DiskImage(mIndex).FileName
            CheckBoxReadOnly.Checked = mEmulator.FloppyContoller.DiskImage(mIndex).IsReadOnly

            ButtonEject.Enabled = True
            ButtonLoad.Enabled = False
        End If
    End Sub
End Class
