' http://jorisvr.nl/retro/
Public MustInherit Class IOPortHandler
    Implements IInterruptController, IIOPortHandler

    Private mRegisteredPorts As List(Of UInt32)

    Public Sub New()
        mRegisteredPorts = New List(Of UInt32)
    End Sub

    Public ReadOnly Property RegisteredPorts As List(Of UInt32) Implements IIOPortHandler.RegisteredPorts
        Get
            Return mRegisteredPorts
        End Get
    End Property

    Public MustOverride Sub Out(port As UInt16, value As Byte) Implements IIOPortHandler.Out
    Public MustOverride Function [In](port As UInt16) As Byte Implements IIOPortHandler.In
    Public MustOverride ReadOnly Property Description As String Implements IIOPortHandler.Description
    Public MustOverride ReadOnly Property Name As String Implements IIOPortHandler.Name
    Public MustOverride Sub Run() Implements IIOPortHandler.Run

    Public Overridable Function GetPendingInterrupt() As Byte Implements IInterruptController.GetPendingInterrupt
        Return -1
    End Function

    Public Overridable Function Read(address As UInt32) As Byte
        Return 0
    End Function

    Public Overridable Sub Write(address As UInt32, value As Byte)
    End Sub
End Class
