Imports x8086NetEmu

Public MustInherit Class VideoAdapter
    Inherits Adapter

    Public Enum VideoModes
        Mode0_Text_BW_40x25 = &H4
        Mode1_Text_Color_40x25 = &H0
        Mode2_Text_BW_80x25 = &H5
        Mode3_Text_Color_80x25 = &H1

        Mode4_Graphic_Color_320x200 = &H2
        Mode5_Graphic_BW_320x200 = &H6
        Mode6_Graphic_Color_640x200 = &H16
        Mode6_Graphic_Color_640x200_Alt = &H12

        Undefined = &HFF
    End Enum

    Public Enum MainModes
        Unknown = -1
        Text = 0
        Graphics = 2
    End Enum

    Public Event KeyDown(sender As Object, e As KeyEventArgs)
    Public Event KeyUp(sender As Object, e As KeyEventArgs)

    Public Overrides ReadOnly Property Type As AdapterType
        Get
            Return AdapterType.Video
        End Get
    End Property

    Public MustOverride Overrides ReadOnly Property Description As String
    Public MustOverride Overrides ReadOnly Property Name As String
    Public MustOverride Overrides ReadOnly Property Vendor As String
    Public MustOverride Overrides ReadOnly Property VersionMajor As Integer
    Public MustOverride Overrides ReadOnly Property VersionMinor As Integer
    Public MustOverride Overrides ReadOnly Property VersionRevision As Integer

    Public MustOverride Property Zoom As Double
    Public MustOverride Property VideoMode As VideoModes

    Public MustOverride Overrides Sub CloseAdapter()
    Public MustOverride Overrides Sub InitiAdapter()
    Public MustOverride Overrides Sub Out(port As UInteger, value As UInteger)
    Public MustOverride Overrides Function [In](port As UInteger) As UInteger
    Public MustOverride Overrides Sub Run()

    Public MustOverride Sub Reset()
    Public MustOverride Sub AutoSize()

    Protected mStartTextVideoAddress As Integer
    Protected mEndTextVideoAddress As Integer

    Protected mStartGraphicsVideoAddress As Integer
    Protected mEndGraphicsVideoAddress As Integer
    Protected mMainMode As MainModes

    Protected mTextResolution As Size = New Size(40, 25)
    Protected mVideoResolution As Size = New Size(0, 0)

    Private Memory(X8086.MemSize - 1) As Boolean

    Protected Overridable Sub OnKeyDown(sender As Object, e As KeyEventArgs)
        RaiseEvent KeyDown(sender, e)
    End Sub

    Protected Overridable Sub OnKeyUp(sender As Object, e As KeyEventArgs)
        RaiseEvent KeyUp(sender, e)
    End Sub

    Public ReadOnly Property StartGraphicsVideoAddress As Integer
        Get
            Return mStartGraphicsVideoAddress
        End Get
    End Property

    Public ReadOnly Property EndGraphicsVideoAddress As Integer
        Get
            Return mEndGraphicsVideoAddress
        End Get
    End Property

    Public ReadOnly Property StartTextVideoAddress As Integer
        Get
            Return mStartTextVideoAddress
        End Get
    End Property

    Public ReadOnly Property EndTextVideoAddress As Integer
        Get
            Return mEndTextVideoAddress
        End Get
    End Property

    Public ReadOnly Property TextResolution As Size
        Get
            Return mTextResolution
        End Get
    End Property

    Public ReadOnly Property GraphicsResolution As Size
        Get
            Return mVideoResolution
        End Get
    End Property

    Public Property IsDirty(address As UInteger) As Boolean
        Get
            Dim r As Boolean = Memory(address)
            Memory(address) = False
            Return r
        End Get
        Set(value As Boolean)
            Memory(address) = value
        End Set
    End Property

    Public ReadOnly Property MainMode As MainModes
        Get
            Return mMainMode
        End Get
    End Property
End Class
