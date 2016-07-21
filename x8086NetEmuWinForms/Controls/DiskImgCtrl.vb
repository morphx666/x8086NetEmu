Imports x8086NetEmu

Public Class DiskImgCtrl
    Private mIndex As Integer
    Private mEmulator As x8086
    Private mHardDiskMode As Boolean

    Private devName As String

    Public Property HardDiskMode As Boolean
        Get
            Return mHardDiskMode
        End Get
        Set(value As Boolean)
            mHardDiskMode = value
            UpdateUI()
        End Set
    End Property

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
            dlg.Title = "Select " + devName + " Disk Image"
            dlg.Filter = "Supported " + devName + " Disk Images|*.ima;*.img;*.vfd|All Files|*.*"
            If dlg.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
                TextBoxImageFileName.Text = dlg.FileName

                MountImage(dlg.FileName, False)
            End If
        End Using
    End Sub

    Private Sub MountImage(fileName As String, ro As Boolean)
        Eject()

        Try
            mEmulator.FloppyContoller.DiskImage(mIndex) = New DiskImage(fileName, ro, mHardDiskMode)
            UpdateUI()
        Catch ex As Exception
            MsgBox("Unable to mount image: " + ex.Message, MsgBoxStyle.Critical)
        End Try
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
        If mHardDiskMode Then
            devName = "Hard"
            Label1.Text = devName + " Drive "
        Else
            devName = "Floppy"
            Label1.Text = devName + " Drive "
        End If

        If mEmulator Is Nothing Then Exit Sub

        If mHardDiskMode Then
            If mIndex >= 128 Then Label1.Text = devName + " Drive " + Chr(67 + mIndex - 128) + ":"
        Else
            Label1.Text = devName + " Drive " + Chr(65 + mIndex) + ":"
        End If

        If mEmulator.FloppyContoller.DiskImage(mIndex) Is Nothing Then
            TextBoxImageFileName.Text = ""

            ButtonEject.Enabled = False
            ButtonLoad.Enabled = True
            CheckBoxReadOnly.Enabled = False
        Else
            TextBoxImageFileName.Text = mEmulator.FloppyContoller.DiskImage(mIndex).FileName
            CheckBoxReadOnly.Checked = mEmulator.FloppyContoller.DiskImage(mIndex).IsReadOnly

            ButtonEject.Enabled = True
            ButtonLoad.Enabled = False
            CheckBoxReadOnly.Enabled = True
        End If
    End Sub

    Private Sub CheckBoxReadOnly_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxReadOnly.CheckedChanged
        If mEmulator.FloppyContoller.DiskImage(mIndex).IsReadOnly <> CheckBoxReadOnly.Checked Then
            MountImage(mEmulator.FloppyContoller.DiskImage(mIndex).FileName, CheckBoxReadOnly.Checked)
        End If
    End Sub
End Class
