Imports x8086NetEmu

Public Class FormDiskManager
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
End Class