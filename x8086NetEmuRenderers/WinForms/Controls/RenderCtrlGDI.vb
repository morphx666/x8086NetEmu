' MODE 0x13: http://www.brackeen.com/vga/basics.html

Public Class RenderCtrlGDI
    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint, True)
        Me.SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
        Me.SetStyle(ControlStyles.ResizeRedraw, True)
        Me.SetStyle(ControlStyles.Selectable, True)
        Me.SetStyle(ControlStyles.UserPaint, True)

        ' Capturing is automatically disabled from the Dispose event

        ' This is used to force the arrow keys to generate a KeyDown event
        ' It also allows us to capture the Alt key
        AddHandler PreviewKeyDown, Sub(sender As Object, e As PreviewKeyDownEventArgs) e.IsInputKey = True
    End Sub
End Class