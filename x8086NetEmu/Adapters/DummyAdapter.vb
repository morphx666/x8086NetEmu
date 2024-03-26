Imports x8086NetEmu

Public Class DummyAdapter
    Inherits Adapter

    Public Overrides ReadOnly Property Type As AdapterType
        Get
            Return AdapterType.Other
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
            Return 1
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return ""
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return ""
        End Get
    End Property

    Public Overrides Sub InitAdapter()
    End Sub

    Public Overrides Sub CloseAdapter()
    End Sub

    Public Overrides Function [In](port As UInt16) As Byte
        Return &HFF
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
    End Sub

    Public Overrides Sub Run()
    End Sub
End Class
