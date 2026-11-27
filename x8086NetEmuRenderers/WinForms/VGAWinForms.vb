' WinForms presenter for the register-level VGAAdapter: the adapter generates complete frames (see
' VGAAdapter.GetFrame); this class only copies them into a bitmap and paints it, scaled to the control.
Public Class VGAWinForms
    Inherits VGAAdapter

    Private videoBMP As New DirectBitmap(1, 1)
    Private ReadOnly bmpLock As New Object()
    Private mRenderControl As Control
    Private repaintPending As Integer

    Public Sub New(cpu As X8086, renderControl As Control)
        MyBase.New(cpu)

        Me.RenderControl = renderControl

        AddHandler mRenderControl.KeyDown, Sub(sender As Object, e As KeyEventArgs)
                                               HandleKeyDown(Me, New XKeyEventArgs(e.KeyValue, e.Modifiers))
                                               e.Handled = True
                                               e.SuppressKeyPress = True
                                           End Sub
        AddHandler mRenderControl.KeyUp, Sub(sender As Object, e As KeyEventArgs)
                                             HandleKeyUp(Me, New XKeyEventArgs(e.KeyValue, e.Modifiers))
                                             e.Handled = True
                                         End Sub

        AddHandler mRenderControl.MouseDown, Sub(sender As Object, e As MouseEventArgs) OnMouseDown(Me, New XMouseEventArgs(e.Button, e.X, e.Y))
        AddHandler mRenderControl.MouseMove, Sub(sender As Object, e As MouseEventArgs)
                                                 If MyBase.CPU.Mouse?.IsCaptured Then
                                                     OnMouseMove(Me, New XMouseEventArgs(e.Button, e.X, e.Y))
                                                     Cursor.Position = mRenderControl.PointToScreen(New Point(cpu.Mouse.MidPointOffset.X, cpu.Mouse.MidPointOffset.Y))
                                                 End If
                                             End Sub
        AddHandler mRenderControl.MouseUp, Sub(sender As Object, e As MouseEventArgs) OnMouseUp(Me, New XMouseEventArgs(e.Button, e.X, e.Y))
    End Sub

    Public Property RenderControl As Control
        Get
            Return mRenderControl
        End Get
        Set(value As Control)
            DetachRenderControl()
            mRenderControl = value
            InitAdapter()
            AddHandler mRenderControl.Paint, AddressOf Paint
        End Set
    End Property

    Protected Sub DetachRenderControl()
        If mRenderControl IsNot Nothing Then RemoveHandler mRenderControl.Paint, AddressOf Paint
    End Sub

    Public Overrides Sub CloseAdapter()
        MyBase.CloseAdapter()
        DetachRenderControl()
    End Sub

    Protected Overrides Sub AutoSize()
        If mRenderControl Is Nothing Then Exit Sub
        If mRenderControl.InvokeRequired Then
            mRenderControl.BeginInvoke(Sub() ResizeRenderControl())
        Else
            ResizeRenderControl()
        End If
    End Sub

    Protected Overrides Sub ResizeRenderControl()
        Dim frm As Form = mRenderControl.FindForm()
        If frm Is Nothing Then Exit Sub
        frm.ClientSize = New Size(CInt(640 * Zoom), CInt(400 * Zoom))
        mRenderControl.Location = Point.Empty
        mRenderControl.Size = frm.ClientSize
    End Sub

    ' Called on the emulation thread after every generated frame
    Protected Overrides Sub Render()
        Dim w As Integer, h As Integer
        Dim frame As Integer() = GetFrame(w, h)
        If frame.Length < w * h Then Exit Sub

        SyncLock bmpLock
            If videoBMP.Width <> w OrElse videoBMP.Height <> h Then
                videoBMP.Dispose()
                videoBMP = New DirectBitmap(w, h)
            End If
            Buffer.BlockCopy(frame, 0, videoBMP.Bits, 0, w * h * 4) ' ARGB ints are BGRA bytes in memory
        End SyncLock

        If mRenderControl IsNot Nothing AndAlso Threading.Interlocked.Exchange(repaintPending, 1) = 0 Then
            Try
                mRenderControl.BeginInvoke(Sub()
                                               Threading.Volatile.Write(repaintPending, 0)
                                               mRenderControl.Invalidate()
                                           End Sub)
            Catch ex As InvalidOperationException
                repaintPending = 0 ' Control not created yet / already disposed
            End Try
        End If
    End Sub

    Private Sub Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half
        g.CompositingMode = Drawing2D.CompositingMode.SourceCopy

        Dim ex As New XPaintEventArgs(g, New XRectangle(e.ClipRectangle.X, e.ClipRectangle.Y, e.ClipRectangle.Width, e.ClipRectangle.Height))
        OnPreRender(sender, ex)
        SyncLock bmpLock
            g.DrawImage(videoBMP.Bitmap, New Rectangle(Point.Empty, mRenderControl.ClientSize))
        End SyncLock
        g.CompositingMode = Drawing2D.CompositingMode.SourceOver
        OnPostRender(sender, ex)
    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "VGA WinForms Adapter"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "VGA WinForms"
        End Get
    End Property
End Class
