' MODE 0x13: http://www.brackeen.com/vga/basics.html
' Color Graphics Adapter (CGA) http://webpages.charter.net/danrollins/techhelp/0066.HTM

' http://www.powernet.co.za/info/BIOS/Mem/
' http://www-ivs.cs.uni-magdeburg.de/~zbrog/asm/memory.html

Public Class CGAWinForms
    Inherits CGAAdapter

    Private blinkCounter As Integer

    Private ReadOnly brushCache(CGAPalette.Length - 1) As Color

    Private ReadOnly preferredFont As String = "Perfect DOS VGA 437"
    Private mFont As Font = New Font(preferredFont, 16, FontStyle.Regular, GraphicsUnit.Pixel)
    Private ReadOnly textFormat As New StringFormat(StringFormat.GenericTypographic)

    Private ReadOnly fontSourceMode As FontSources
    Private g As Graphics

    Private scale As New SizeF(1, 1)

    Private videoBMP As New DirectBitmap(1, 1)
    Private ReadOnly charsCache As New List(Of VideoChar)
    Private ReadOnly charSizeCache As New Dictionary(Of Integer, Size)

    Private mRenderControl As Control

    Public Sub New(cpu As X8086, renderControl As Control, Optional fontSource As FontSources = FontSources.BitmapFile, Optional bitmapFontFile As String = "")
        MyBase.New(cpu)
        fontSourceMode = fontSource

        Me.RenderControl = renderControl

        SetupEventHandlers()

        Dim fontCGAPath As String = X8086.FixPath("misc\" + bitmapFontFile)
        Dim fontCGAError As String = ""

        Select Case fontSource
            Case FontSources.BitmapFile
                If IO.File.Exists(fontCGAPath) Then
                    Try
                        VideoChar.FontBitmaps = IO.File.ReadAllBytes(fontCGAPath)
                        mCellSize = New XSize(8, 16)
                    Catch ex As Exception
                        fontCGAError = ex.Message
                        fontSourceMode = FontSources.TrueType
                    End Try
                Else
                    fontCGAError = "File not found"
                    fontSourceMode = FontSources.TrueType
                End If
            Case FontSources.ROM
                CellSize = New XSize(8, 4)
                VideoChar.BuildFontBitmapsFromROM(8, 4, 4, &HFE000 + &H1A6D, cpu.Memory)
                mCellSize = New XSize(8, 8)
        End Select

        If fontSourceMode = FontSources.TrueType Then
            If mFont.Name <> preferredFont Then
                MsgBox(If(fontSource = FontSources.BitmapFile, "ASCII VGA Font Data not found at '" + fontCGAPath + "'" + If(fontCGAError <> "", ": " + fontCGAError, "") +
                       vbCrLf + vbCrLf, "") +
                       "CGAWinForms requires the '" + preferredFont + "' font. Please install it before using this adapter", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                mFont = New Font("Consolas", 16, FontStyle.Regular, GraphicsUnit.Pixel)
                If mFont.Name <> "Consolas" Then
                    mFont = New Font("Andale Mono", 16, FontStyle.Regular, GraphicsUnit.Pixel)
                    If mFont.Name <> "Andale Mono" Then
                        mFont = New Font("Courier New", 16, FontStyle.Regular, GraphicsUnit.Pixel)
                    End If
                End If
            End If
        End If

        textFormat.FormatFlags = StringFormatFlags.NoWrap Or
                                 StringFormatFlags.MeasureTrailingSpaces Or
                                 StringFormatFlags.FitBlackBox Or
                                 StringFormatFlags.NoClip
    End Sub

    Private Sub SetupEventHandlers()
        AddHandler mRenderControl.KeyDown, Sub(sender As Object, e As KeyEventArgs)
                                               HandleKeyDown(Me, New XKeyEventArgs(e.KeyValue, e.Modifiers))

                                               ' FIXME: Is there a better way to do this?
                                               e.Handled = True
                                               e.SuppressKeyPress = True
                                           End Sub
        AddHandler mRenderControl.KeyUp, Sub(sender As Object, e As KeyEventArgs)
                                             HandleKeyUp(Me, New XKeyEventArgs(e.KeyValue, e.Modifiers))

                                             ' FIXME: Is there a better way to do this?
                                             e.Handled = True
                                         End Sub

        AddHandler mRenderControl.MouseDown, Sub(sender As Object, e As MouseEventArgs) OnMouseDown(Me, New XMouseEventArgs(e.Button, e.X, e.Y))
        AddHandler mRenderControl.MouseMove, Sub(sender As Object, e As MouseEventArgs)
                                                 If CPU.Mouse?.IsCaptured Then
                                                     OnMouseMove(Me, New XMouseEventArgs(e.Button, e.X, e.Y))
                                                     Cursor.Position = mRenderControl.PointToScreen(New Point(CPU.Mouse.MidPointOffset.X, CPU.Mouse.MidPointOffset.Y))
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

    Public Overrides Sub InitAdapter()
        If Not isInit Then
            MyBase.InitAdapter()
            Task.Run(action:=Async Sub()
                                 Do
                                     Await Task.Delay(2 * 1000 / VERTSYNC)
                                     mRenderControl.Invoke(Sub() mRenderControl.Invalidate())
                                 Loop Until X8086.IsClosing
                             End Sub)
        End If
    End Sub

    Public Overrides Sub CloseAdapter()
        MyBase.CloseAdapter()
        DetachRenderControl()
    End Sub

    Protected Overrides Sub AutoSize()
        If mRenderControl IsNot Nothing Then
            If mRenderControl.InvokeRequired Then
                mRenderControl.Invoke(Sub() ResizeRenderControl())
            Else
                ResizeRenderControl()
            End If
        End If
    End Sub

    Protected Overrides Sub ResizeRenderControl()
        Dim ctrlSize As Size

        If MainMode = MainModes.Text Then
            ctrlSize = New Size(mCellSize.Width * TextResolution.Width, mCellSize.Height * TextResolution.Height)
        Else
            ctrlSize = New Size(GraphicsResolution.Width, GraphicsResolution.Height)
        End If

        Dim frmSize As New Size(640 * Zoom, 400 * Zoom)
        Dim frm As Form = mRenderControl.FindForm
        frm.ClientSize = frmSize
        mRenderControl.Location = Point.Empty
        mRenderControl.Size = frmSize
        If mCellSize.Width = 0 OrElse mCellSize.Height = 0 Then Exit Sub

        scale = New SizeF(frmSize.Width / ctrlSize.Width, frmSize.Height / ctrlSize.Height)
    End Sub

    Private Sub Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics

        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighSpeed
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.CompositingQuality = Drawing2D.CompositingQuality.HighSpeed

        g.ScaleTransform(scale.Width, scale.Height)

        Dim ex = New XPaintEventArgs(g, New XRectangle(e.ClipRectangle.X,
                                                       e.ClipRectangle.Y,
                                                       e.ClipRectangle.Width,
                                                       e.ClipRectangle.Height))

        OnPreRender(sender, ex)
        g.CompositingMode = Drawing2D.CompositingMode.SourceCopy

        SyncLock chars
            g.DrawImageUnscaled(videoBMP, 0, 0)
        End SyncLock

        g.CompositingMode = Drawing2D.CompositingMode.SourceOver
        OnPostRender(sender, ex)
    End Sub

    Protected Overrides Sub OnPaletteRegisterChanged()
        MyBase.OnPaletteRegisterChanged()

        If brushCache IsNot Nothing Then
            For i As Integer = 0 To CGAPalette.Length - 1
                brushCache(i) = CGAPalette(i).ToColor()
            Next

            charsCache.Clear()
        End If
    End Sub

    Protected Overrides Sub Render()
        If VideoEnabled Then
            Select Case MainMode
                Case MainModes.Text
                    SyncLock chars
                        RenderText()
                    End SyncLock

                Case MainModes.Graphics
                    RenderGraphics()

            End Select
        End If
    End Sub

    Private Sub RenderText()
        Dim b0 As Byte
        Dim b1 As Byte

        Dim col As Integer = 0
        Dim row As Integer = 0

        Dim r As New Rectangle(Point.Empty, mCellSize.ToSize())

        If Not CursorVisible Then blinkCounter = 2 * BlinkRate

        For address As Integer = mStartTextVideoAddress To mEndTextVideoAddress - 2 Step 2
            b0 = CPU.Memory(address)
            b1 = CPU.Memory(address + 1)

            If BlinkCharOn AndAlso (b1 And &B1000_0000) <> 0 Then
                If blinkCounter < BlinkRate Then b0 = 0
            End If

            RenderChar(b0, videoBMP, brushCache(b1 And &HF), brushCache(b1 >> 4), r.Location)

            If CursorVisible AndAlso row = CursorRow AndAlso col = CursorCol Then
                If blinkCounter < BlinkRate Then
                    videoBMP.FillRectangle(brushCache(b1 And &HF),
                                           r.X + 0, r.Y - 1 + mCellSize.Height - (CursorEnd - CursorStart) - 1,
                                           mCellSize.Width, CursorEnd - CursorStart + 1)
                End If

                If blinkCounter >= 2 * BlinkRate Then
                    blinkCounter = 0
                Else
                    blinkCounter += 1
                End If
            End If

            r.X += mCellSize.Width
            col += 1
            If col = TextResolution.Width Then
                col = 0
                row += 1
                If row = TextResolution.Height Then Exit For

                r.X = 0
                r.Y += mCellSize.Height
            End If
        Next
    End Sub

    Private Sub RenderGraphics()
        Dim b As Byte
        Dim xDiv As Integer = If(PixelsPerByte = 4, 2, 3)

        For y As Integer = 0 To GraphicsResolution.Height - 1
            Dim cy As Integer = ((y >> 1) * 80) + ((y And 1) * &H2000)
            For x As Integer = 0 To GraphicsResolution.Width - 1
                b = CPU.Memory(mStartGraphicsVideoAddress + cy + (x >> xDiv))

                If PixelsPerByte = 4 Then
                    Select Case x And 3
                        Case 3 : b = b And 3
                        Case 2 : b = (b >> 2) And 3
                        Case 1 : b = (b >> 4) And 3
                        Case 0 : b = (b >> 6) And 3
                    End Select
                Else
                    b = (b >> (7 - (x And 7))) And 1
                End If

                videoBMP.Pixel(x, y) = CGAPalette(b).ToColor()
            Next
        Next
    End Sub

    Private Sub RenderChar(c As Integer, dbmp As DirectBitmap, fb As Color, bb As Color, p As Point)
        If fontSourceMode = FontSources.TrueType Then
            Using bbb As New SolidBrush(bb)
                g.FillRectangle(bbb, New Rectangle(p, mCellSize.ToSize()))
                Using bfb As New SolidBrush(fb)
                    g.DrawString(Char.ConvertFromUtf32(c), mFont, bfb, p.X - mCellSize.Width / 2 + 2, p.Y)
                End Using
            End Using
        Else
            Dim ccc As New VideoChar(c, fb, bb)
            Dim idx As Integer = charsCache.IndexOf(ccc)
            If idx = -1 Then
                ccc.Render(mCellSize.Width, mCellSize.Height)
                charsCache.Add(ccc)
                idx = charsCache.Count - 1
            End If
            charsCache(idx).Paint(dbmp, p, scale)
        End If
    End Sub

    Private Function MeasureChar(g As Graphics, code As Integer, text As Char, font As Font) As Size
        Dim size As Size

        Select Case fontSourceMode
            Case FontSources.BitmapFile
                charSizeCache.Add(code, mCellSize.ToSize())
            Case FontSources.TrueType
                If charSizeCache.ContainsKey(code) Then Return charSizeCache(code)

                Dim r As New RectangleF(0, 0, 1000, 1000)
                Dim ranges() As CharacterRange = {New CharacterRange(0, 1)}
                Dim regions() As Region

                textFormat.SetMeasurableCharacterRanges(ranges)

                regions = g.MeasureCharacterRanges(text, font, r, textFormat)
                r = regions(0).GetBounds(g)

                size = New Size(r.Right - 1, r.Bottom)
                charSizeCache.Add(code, size)
            Case FontSources.ROM
                size = New Size(8, 8)
                charSizeCache.Add(code, size)
        End Select

        Return size
    End Function

    Public Overrides ReadOnly Property Description As String
        Get
            Return "CGA WinForms Adapter"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "CGA WinForms"
        End Get
    End Property

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
        MyBase.InitVideoMemory(clearScreen)

        If mRenderControl IsNot Nothing Then
            If clearScreen OrElse charSizeCache.Count = 0 Then
                charSizeCache.Clear()
                Using g As Graphics = mRenderControl.CreateGraphics()
                    For i As Integer = 0 To 255
                        MeasureChar(g, i, chars(i), mFont)
                    Next
                End Using
            End If

            ' Monospace... duh!
            Dim cs As Size = charSizeCache(65)
            mCellSize = New XSize(cs.Width, cs.Height)
        End If

        SyncLock chars
            If videoBMP IsNot Nothing Then videoBMP.Dispose()
            Select Case MainMode
                Case MainModes.Text
                    videoBMP = New DirectBitmap(640, 400)
                Case MainModes.Graphics
                    videoBMP = New DirectBitmap(GraphicsResolution.Width, GraphicsResolution.Height)
            End Select
        End SyncLock

        If fontSourceMode = FontSources.TrueType Then
            If g IsNot Nothing Then g.Dispose()
            g = Graphics.FromImage(videoBMP)
        End If
    End Sub
End Class