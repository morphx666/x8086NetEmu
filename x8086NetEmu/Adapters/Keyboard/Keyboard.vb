Public Class KeyboardAdapter
    Inherits Adapter
    Implements IExternalInputHandler

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)
    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Simple Keyboard Driver Emulator"
        End Get
    End Property

    Public Overrides ReadOnly Property Vendor As String
        Get
            Return "xFX JumpStart"
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMajor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMinor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionRevision As Integer
        Get
            Return 19
        End Get
    End Property

    Public Overrides ReadOnly Property Type As AdapterType
        Get
            Return AdapterType.Keyboard
        End Get
    End Property

    Public Overrides Sub InitAdapter()
    End Sub

    Public Overrides Sub CloseAdapter()
    End Sub

    Public Overrides Function [In](port As UInt16) As Byte
        Return &HFF
    End Function

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Keyboard"
        End Get
    End Property

    Public Overrides Sub Out(port As UInt16, value As Byte)
    End Sub

    Public Overrides Sub Run()
    End Sub

    Public Sub HandleInput(e As ExternalInputEvent) Implements IExternalInputHandler.HandleInput
        Dim keyEvent As XKeyEventArgs = CType(e.Event, XKeyEventArgs)
        Dim isUp As Boolean = e.Extra

        If CPU.PPI IsNot Nothing Then CPU.PPI.PutKeyData(keyEvent.KeyValue And &HFF, isUp)
    End Sub
End Class
