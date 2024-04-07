Public Class ButtonIcon
    Inherits Button

    Private mouseIsOver As Boolean
    Private disabledImage As x8086NetEmuRenderers.DirectBitmap

    Public Sub New()
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint, True)
        Me.SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
        Me.SetStyle(ControlStyles.ResizeRedraw, True)
        Me.SetStyle(ControlStyles.Selectable, True)
        Me.SetStyle(ControlStyles.UserPaint, True)

        AddHandler Me.MouseEnter, Sub()
                                      mouseIsOver = True
                                      Me.Invalidate()
                                  End Sub

        AddHandler Me.MouseLeave, Sub()
                                      mouseIsOver = False
                                      Me.Invalidate()
                                  End Sub
    End Sub

    Public Overloads Property Image As Image
        Get
            Return MyBase.Image
        End Get
        Set(value As Image)
            MyBase.Image = value
            GenerateDisabledImage()
        End Set
    End Property

    Private Sub GenerateDisabledImage()
        Dim disabledColor As Color = Color.FromArgb(66, 66, 66)

        If disabledImage IsNot Nothing Then
            disabledImage.Dispose()
            disabledImage = Nothing
        End If

        If Me.Image IsNot Nothing Then
            disabledImage = New x8086NetEmuRenderers.DirectBitmap(Me.Image)

            For y As Integer = 0 To disabledImage.Height - 1
                For x As Integer = 0 To disabledImage.Width - 1
                    If disabledImage.Pixel(x, y).A <> 0 Then disabledImage.Pixel(x, y) = disabledColor
                Next
            Next
        End If
    End Sub

    Protected Overrides Sub OnPaintBackground(pevent As PaintEventArgs)
        'MyBase.OnPaintBackground(pevent)
    End Sub

    Protected Overrides Sub OnPaint(pevent As PaintEventArgs)
        If Me.Image Is Nothing Then Exit Sub

        Dim g As Graphics = pevent.Graphics
        Dim r As Rectangle = Me.DisplayRectangle
        r.Width -= 1
        r.Height -= 1

        If mouseIsOver AndAlso Me.Enabled Then
            g.Clear(Me.FlatAppearance.MouseOverBackColor)
        Else
            g.Clear(Me.BackColor)
        End If

        If Me.Enabled Then
            If Me.Image IsNot Nothing Then g.DrawImage(Me.Image, 0, 0, r.Width, r.Height)
        Else
            If disabledImage IsNot Nothing Then g.DrawImage(disabledImage.Bitmap, 0, 0, r.Width, r.Height)
        End If
    End Sub
End Class
