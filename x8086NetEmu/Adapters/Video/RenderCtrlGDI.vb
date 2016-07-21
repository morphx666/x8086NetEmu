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

        ' This is used to force the arrows keys to generate a KeyDown event
        ' It also allows us to capture the Alt key
        AddHandler PreviewKeyDown, Sub(sender As Object, e As PreviewKeyDownEventArgs) e.IsInputKey = True
    End Sub

    Protected Overrides Sub OnPaintBackground(e As System.Windows.Forms.PaintEventArgs)
        'MyBase.OnPaintBackground(e)
    End Sub

    ' This method also works
    'Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
    '    Select Case keyData
    '        Case Keys.Up, Keys.Down, Keys.Left, Keys.Right
    '            OnKeyDown(New KeyEventArgs(keyData))
    '    End Select
    '    Return MyBase.ProcessCmdKey(msg, keyData)
    'End Function
End Class
