Imports x8086NetEmu

Public Class FormDiskManager
    Private mEmulator As x8086
    Private mHardDiskMode As Boolean

    Public Property HardDiskMode As Boolean
        Get
            Return mHardDiskMode
        End Get
        Set(value As Boolean)
            mHardDiskMode = value

            DiskImgCtrl1.HardDiskMode = HardDiskMode
            DiskImgCtrl1.Index = If(mHardDiskMode, 128, 0)

            DiskImgCtrl2.HardDiskMode = HardDiskMode
            DiskImgCtrl2.Index = If(mHardDiskMode, 129, 1)

            Me.Text = If(mHardDiskMode, "Hard", "Floppy") + " Disk Manager"
        End Set
    End Property

    Public Property Emulator As x8086
        Get
            Return mEmulator
        End Get
        Set(value As x8086)
            mEmulator = value

            DiskImgCtrl1.Emulator = mEmulator
            DiskImgCtrl2.Emulator = mEmulator
        End Set
    End Property
End Class