Imports x8086NetEmu

Public Class FormFloppyManager
    Private mEmulator As x8086

    Public Property Emulator As x8086
        Get
            Return mEmulator
        End Get
        Set(value As x8086)
            mEmulator = value

            FloppyImgCtrl1.Emulator = mEmulator
            FloppyImgCtrl2.Emulator = mEmulator
        End Set
    End Property
End Class