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
    Public MustOverride Overrides Sub Out(port As Integer, value As Integer)
    Public MustOverride Overrides Sub Run()

    Public MustOverride Sub Reset()
    Public MustOverride Sub AutoSize()

    Public MustOverride Overrides Function [In](port As Integer) As Integer

    Protected Overridable Sub OnKeyDown(sender As Object, e As KeyEventArgs)
        RaiseEvent KeyDown(sender, e)
    End Sub

    Protected Overridable Sub OnKeyUp(sender As Object, e As KeyEventArgs)
        RaiseEvent KeyUp(sender, e)
    End Sub
End Class
