Imports x8086NetEmu

Public Class FormMediaManager
    Private mEmulator As x8086
    Private mHardDiskMode As Boolean

    Public Property Emulator As x8086
        Get
            Return mEmulator
        End Get
        Set(value As x8086)
            mEmulator = value

            DiskImgCtrlA.Emulator = mEmulator
            DiskImgCtrlB.Emulator = mEmulator
            DiskImgCtrlC.Emulator = mEmulator
            DiskImgCtrlD.Emulator = mEmulator
        End Set
    End Property

    Private Sub FormDiskManager_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        AddHandler ButtonOK.Click, Sub()
                                       Me.DialogResult = DialogResult.OK
                                       Me.Close()
                                   End Sub
        AddHandler ButtonReboot.Click, Sub()
                                           Me.DialogResult = DialogResult.Abort
                                           Me.Close()
                                       End Sub
    End Sub
End Class