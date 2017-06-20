' MODE 0x13: http://www.brackeen.com/vga/basics.html
' Color Graphics Adapter (CGA) http://webpages.charter.net/danrollins/techhelp/0066.HTM

Public Class CGAWinForms
    Inherits CGAAdapter

    Private Class CGAChar
        Private mCGAChar As Integer
        Private mForeColor As Color
        Private mBackColor As Color
        Private mBitmap As DirectBitmap

        Public Sub New(c As Integer, fb As Color, bb As Color)
            mCGAChar = c
            mForeColor = fb
            mBackColor = bb
        End Sub

        Public ReadOnly Property CGAChar As Integer
            Get
                Return mCGAChar
            End Get
        End Property

        Public ReadOnly Property ForeColor As Color
            Get
                Return mForeColor
            End Get
        End Property

        Public ReadOnly Property BackColor As Color
            Get
                Return mBackColor
            End Get
        End Property

        Public Sub Paint(dbmp As DirectBitmap, p As Point, scale As SizeF)
            Dim w4s As Integer = mBitmap.Width * 4
            Dim w4d As Integer = dbmp.Width * 4
            p.X *= 4
            For y As Integer = 0 To mBitmap.Height - 1
                Array.Copy(mBitmap.Bits, y * w4s, dbmp.Bits, (y + p.Y) * w4d + p.X, w4s)
            Next
        End Sub

        Public Sub Render()
            If mBitmap Is Nothing Then
                mBitmap = New DirectBitmap(8, 16)

                For y As Integer = 0 To 16 - 1
                    For x As Integer = 0 To 8 - 1
                        If fontCGA(mCGAChar * 128 + y * 8 + x) = 1 Then
                            mBitmap.Pixel(x, y) = mForeColor
                        Else
                            mBitmap.Pixel(x, y) = mBackColor
                        End If
                    Next
                Next
            End If
        End Sub

        Public Shared Operator =(c1 As CGAChar, c2 As CGAChar) As Boolean
            Return c1.CGAChar = c2.CGAChar AndAlso
                    c1.ForeColor = c2.ForeColor AndAlso
                    c1.BackColor = c2.BackColor
        End Operator

        Public Shared Operator <>(c1 As CGAChar, c2 As CGAChar) As Boolean
            Return Not (c1 = c2)
        End Operator

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me = CType(obj, CGAChar)
        End Function

        Public Overrides Function ToString() As String
            Return String.Format("{0:000} [{1:000}:{2:000}:{3:000}] [{4:000}:{5:000}:{6:000}]",
                                 mCGAChar,
                                 mForeColor.R,
                                 mForeColor.G,
                                 mForeColor.B,
                                 mBackColor.R,
                                 mBackColor.G,
                                 mBackColor.B)
        End Function
    End Class
    Private cgaCharsCache As New List(Of CGAChar)
    Private videoBMP As DirectBitmap

    Private charSize As Size
    Private blinkCounter As Integer

    Private preferredFont As String = "Perfect DOS VGA 437"
    Private mFont As Font = New Font(preferredFont, 16, FontStyle.Regular, GraphicsUnit.Pixel)
    Private textFormat As StringFormat = New StringFormat(StringFormat.GenericTypographic)

    Private charSizeCache As New Dictionary(Of Integer, Size)

    Private brushCache(16 - 1) As Color
    Private cursorBrush As Color = Color.FromArgb(128, Color.White)

    Private Shared fontCGA() As Byte
    Private useCGAFont As Boolean

    Private scale As New SizeF(1, 1)

    Private mCPU As x8086
    Private mRenderControl As Control
    Private mHideHostCursor As Boolean = True

    Public Event PreRender(sender As Object, e As PaintEventArgs)
    Public Event PostRender(sender As Object, e As PaintEventArgs)

    Private Class TaskSC
        Inherits Scheduler.Task

        Public Sub New(owner As IOPortHandler)
            MyBase.New(owner)
        End Sub

        Public Overrides Sub Run()
            Owner.Run()
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return Owner.Name
            End Get
        End Property
    End Class
    Private task As Scheduler.Task = New TaskSC(Me)

    Public Sub New(cpu As x8086, renderControl As Control, Optional tryUseCGAFont As Boolean = True)
        MyBase.New(cpu)
        useCGAFont = tryUseCGAFont
        mCPU = cpu
        Me.RenderControl = renderControl

        AddHandler mRenderControl.KeyDown, Sub(sender As Object, e As KeyEventArgs) HandleKeyDown(Me, e)
        AddHandler mRenderControl.KeyUp, Sub(sender As Object, e As KeyEventArgs) HandleKeyUp(Me, e)

        AddHandler mRenderControl.MouseDown, Sub(sender As Object, e As MouseEventArgs) OnMouseDown(Me, e)
        AddHandler mRenderControl.MouseMove, Sub(sender As Object, e As MouseEventArgs) OnMouseMove(Me, e)
        AddHandler mRenderControl.MouseUp, Sub(sender As Object, e As MouseEventArgs) OnMouseUp(Me, e)

        Dim fontCGAPath As String = x8086.FixPath("roms\asciivga.dat")
        Dim fontCGAError As String = ""

        If useCGAFont Then
            If IO.File.Exists(fontCGAPath) Then
                Try
                    fontCGA = IO.File.ReadAllBytes(fontCGAPath)
                Catch ex As Exception
                    fontCGAError = ex.Message
                    useCGAFont = False
                End Try
            Else
                fontCGAError = "File not found"
                useCGAFont = False
            End If
        End If

        If Not useCGAFont Then
            If mFont.Name <> preferredFont Then
                MsgBox(If(useCGAFont, "ASCII VGA Font Data not found at '" + fontCGAPath + "'" + If(fontCGAError <> "", ": " + fontCGAError, "") +
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

    Public Property RenderControl As Control
        Get
            Return mRenderControl
        End Get
        Set(value As Control)
            DetachRenderControl()
            mRenderControl = value

            'useSDL = TypeOf mRenderControl Is RenderCtrlSDL
            'If useSDL Then
            '    sdlCtrl = CType(mRenderControl, RenderCtrlSDL)
            '    sdlCtrl.Init(Me, mFont.FontFamily.Name, mFont.Size)
            'End If

            InitiAdapter()

            AddHandler mRenderControl.Paint, AddressOf Paint
        End Set
    End Property

    Protected Sub DetachRenderControl()
        If mRenderControl IsNot Nothing Then RemoveHandler mRenderControl.Paint, AddressOf Paint
    End Sub

    Public Overrides Sub CloseAdapter()
        MyBase.CloseAdapter()

        DisposeColorCaches()
        DetachRenderControl()
    End Sub

    Public Overrides Sub AutoSize()
        If mRenderControl IsNot Nothing Then
            If mRenderControl.InvokeRequired Then
                mRenderControl.Invoke(New MethodInvoker(AddressOf ResizeRenderControl))
            Else
                ResizeRenderControl()
            End If
        End If
    End Sub

    Private Sub ResizeRenderControl()
        Dim ctrlSize As Size

        If MainMode = MainModes.Text Then
            '    'Using g As Graphics = mRenderControl.CreateGraphics()
            ctrlSize = New Size(charSize.Width * TextResolution.Width, charSize.Height * TextResolution.Height)
            '    'End Using
        Else
            ctrlSize = New Size(GraphicsResolution.Width, GraphicsResolution.Height)
        End If

        Dim frmSize = New Size(640 * Zoom, 400 * Zoom)
        mRenderControl.FindForm.ClientSize = frmSize
        mRenderControl.Size = frmSize
        If charSize.Width = 0 OrElse charSize.Height = 0 Then Exit Sub

        scale = New SizeF(frmSize.Width / ctrlSize.Width, frmSize.Height / ctrlSize.Height)
    End Sub

    Protected Overrides Sub Render()
        mRenderControl.Invalidate()
    End Sub

    Private Sub Paint(sender As Object, e As PaintEventArgs)
        SyncLock MyBase.lockObject
            Dim g As Graphics = e.Graphics

            g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighSpeed
            g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
            g.CompositingQuality = Drawing2D.CompositingQuality.HighSpeed

            g.ScaleTransform(scale.Width, scale.Height)

            RaiseEvent PreRender(sender, e)
            g.CompositingMode = Drawing2D.CompositingMode.SourceCopy

            Select Case MainMode
                Case MainModes.Text
                    'If useSDL Then
                    '    RenderTextSDL()
                    'Else
                    RenderText()
                    'End If
                Case MainModes.Graphics
                    RenderGraphics()
            End Select

            g.DrawImageUnscaled(videoBMP, 0, 0)

            g.CompositingMode = Drawing2D.CompositingMode.SourceOver
            RaiseEvent PostRender(sender, e)

            'RenderWaveform(g)
        End SyncLock
    End Sub

    Protected Overrides Sub OnPaletteRegisterChanged()
        MyBase.OnPaletteRegisterChanged()

        SyncLock MyBase.lockObject
            DisposeColorCaches()
            For i As Integer = 0 To CGAPalette.Length - 1
                brushCache(i) = CGAPalette(i)
            Next
        End SyncLock
    End Sub

    Private Sub RenderGraphics()
        Dim b As Byte
        Dim pixelsPerByte As Integer = If(VideoMode = VideoModes.Mode6_Graphic_Color_640x200, 8, 4)
        Dim yOffset As Integer
        Dim yRenderOffset As Integer
        Dim v As Byte
        Dim address As UInteger

        For y As Integer = 0 To 200 - 1
            If y < 100 Then ' Even Scan Lines
                yOffset = StartGraphicsVideoAddress + y * 80
                yRenderOffset = y * 2
            Else            ' Odd Scan Lines
                yOffset = StartGraphicsVideoAddress + (y Mod 100) * 80 + &H2000
                yRenderOffset = (y Mod 100) * 2 + 1
            End If

            For x As Integer = 0 To 80 - 1
                address = x + yOffset
                If Not MyBase.IsDirty(address) Then Continue For
                b = CPU.RAM(address)

                For pixel As Integer = 0 To pixelsPerByte - 1
                    If VideoMode = VideoModes.Mode4_Graphic_Color_320x200 Then
                        Select Case pixel And 3
                            Case 3 : v = b And 3
                            Case 2 : v = (b >> 2) And 3
                            Case 1 : v = (b >> 4) And 3
                            Case 0 : v = (b >> 6) And 3
                        End Select
                    Else
                        v = (b >> (7 - (pixel And 7))) And 1
                    End If

                    'If mVideoMode = VideoModes.Mode4_Graphic_Color_320x200 Then
                    'b *= 2
                    'Else
                    'b *= 63
                    'End If
                    videoBMP.Pixel(x * pixelsPerByte + pixel, yRenderOffset) = CGAPalette(v)
                Next
            Next
        Next
    End Sub

    Private Sub RenderText()
        Dim b0 As Byte
        Dim b1 As Byte

        Dim col As Integer = 0
        Dim row As Integer = 0

        Static cursorAddress As New List(Of Integer)

        Dim r As New Rectangle(Point.Empty, charSize)

        For address As Integer = StartTextVideoAddress To EndTextVideoAddress Step 2
            b0 = mCPU.Memory(address)
            b1 = mCPU.Memory(address + 1)

            If BlinkCharOn AndAlso (b1 And &B1000_0000) Then
                If (blinkCounter < BlinkRate) Then b0 = 0
                MyBase.IsDirty(address) = True
            End If

            If MyBase.IsDirty(address) OrElse MyBase.IsDirty(address + 1) OrElse cursorAddress.Contains(address) Then
                RenderChar(b0, videoBMP, brushCache(b1.LowNib()), brushCache(b1.HighNib()), r.Location)
                cursorAddress.Remove(address)
            End If

            If CursorVisible AndAlso row = CursorRow AndAlso col = CursorCol Then
                If (blinkCounter < BlinkRate AndAlso CursorVisible) Then
                    videoBMP.FillRectangle(brushCache(b1.LowNib()),
                                           r.X + 1, r.Y + charSize.Height - (MyBase.CursorEnd - MyBase.CursorStart) - 2,
                                           charSize.Width - 1, (MyBase.CursorEnd - MyBase.CursorStart))
                    cursorAddress.Add(address)
                End If

                If blinkCounter >= 2 * BlinkRate Then
                    blinkCounter = 0
                Else
                    blinkCounter += 1
                End If
            End If

            r.X += charSize.Width
            col += 1
            If col = TextResolution.Width Then
                col = 0
                row += 1
                If row = TextResolution.Height Then Exit For

                r.X = 0
                r.Y += charSize.Height
            End If
        Next
    End Sub

    Public Function ColRowToRectangle(col As Integer, row As Integer) As Rectangle
        Return New Rectangle(New Point(col * charSize.Width, row * charSize.Height), charSize)
    End Function

    Public Function ColRowToAddress(col As Integer, row As Integer) As Integer
        Return StartTextVideoAddress + row * (TextResolution.Width * 2) + (col * 2)
    End Function

    Private Sub RenderChar(c As Integer, dbmp As DirectBitmap, fb As Color, bb As Color, p As Point)
        Dim ccc As New CGAChar(c, fb, bb)
        Dim idx As Integer = cgaCharsCache.IndexOf(ccc)
        If idx = -1 Then
            ccc.Render()
            cgaCharsCache.Add(ccc)
            idx = cgaCharsCache.Count - 1
        End If
        cgaCharsCache(idx).Paint(dbmp, p, scale)
    End Sub

    Private Sub RenderWaveform(g As Graphics)
#If Win32 Then
        If mCPU.PIT.Speaker IsNot Nothing Then
            g.ResetTransform()

            Dim h As Integer = mRenderControl.Height * 0.6
            Dim h2 As Integer = h / 2
            Dim p1 As Point = New Point(0, mCPU.PIT.Speaker.AudioBuffer(0) / Byte.MaxValue * h + h * 0.4)
            Dim p2 As Point
            Dim len As Integer = mCPU.PIT.Speaker.AudioBuffer.Length

            Using p As New Pen(Brushes.Red, 3)
                For i As Integer = 1 To len - 1
                    Try
                        p2 = New Point(i / len * mRenderControl.Width, mCPU.PIT.Speaker.AudioBuffer(i) / Byte.MaxValue * h + h * 0.4)
                        g.DrawLine(p, p1, p2)
                        p1 = p2
                    Catch
                        Exit For
                    End Try
                Next
            End Using
        End If
#End If
    End Sub

    Private Function MeasureChar(graphics As Graphics, code As Integer, text As Char, font As Font) As Size
        Dim size As Size

        If useCGAFont Then
            size = New Size(8, 16)
            charSizeCache.Add(code, size)
        Else
            If charSizeCache.ContainsKey(code) Then Return charSizeCache(code)

            Dim rect As RectangleF = New RectangleF(0, 0, 1000, 1000)
            Dim ranges() As CharacterRange = {New CharacterRange(0, 1)}
            Dim regions() As Region = {New Region()}

            textFormat.SetMeasurableCharacterRanges(ranges)

            regions = graphics.MeasureCharacterRanges(text, font, rect, textFormat)
            rect = regions(0).GetBounds(graphics)

            size = New Size(rect.Right - 1, rect.Bottom)
            charSizeCache.Add(code, size)
        End If

        Return size
    End Function

    Private Sub DisposeColorCaches()
    End Sub

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

    ' http://www.powernet.co.za/info/BIOS/Mem/
    ' http://www-ivs.cs.uni-magdeburg.de/~zbrog/asm/memory.html
    Private Sub UpdateSystemInformationArea()
        '' Display Mode
        'Emulator.RAM8(&H40, &H49) = CByte(MyBase.VideoMode)

        '' Number of columns on screen
        'Emulator.RAM16(&H40, &H4A) = TextResolution.Width

        '' Length of Regen Buffer
        'Emulator.RAM16(&H40, &H4C) = MyBase.EndTextVideoAddress - MyBase.StartTextVideoAddress

        '' Current video page start address in video memory (after 0B800:0000)
        '' Starting Address of Regen Buffer. Offset from the beginning of the display adapter memory
        'Emulator.RAM16(&H40, &H4E) = &HB800

        '' Current video page start address in video memory (after 0B800:0000)
        'For i As Integer = 0 To 1 'MyBase.pagesCount
        '    Emulator.RAM8(&H40, &H50 + i * 2 + 0) = CursorCol
        '    Emulator.RAM8(&H40, &H50 + i * 2 + 1) = CursorRow
        'Next

        '' Cursor Start and End Scan Lines
        'Emulator.RAM8(&H40, &H60) = 0 ' ????????
        'Emulator.RAM8(&H40, &H61) = 0 ' ????????

        '' Current Display Page
        'Emulator.RAM8(&H40, &H62) = 1 'activePage

        '' CRT Controller Base Address 
        'Emulator.RAM16(&H40, &H63) = &H3D4

        '' Current Setting of the Mode Control Register
        'Emulator.RAM8(&H40, &H65) = x8086.BitsArrayToWord(CGAModeControlRegister)

        '' Current Setting of the Color Select Register
        'Emulator.RAM16(&H40, &H66) = Emulator.RAM16(&H40, &H63) + 5

        '' Rows on screen minus one
        'Emulator.RAM8(&H40, &H84) = TextResolution.Height - 1
    End Sub

    Public Overrides Sub Run()
        If mRenderControl IsNot Nothing Then mRenderControl.Invalidate()
    End Sub

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
        MyBase.InitVideoMemory(clearScreen)

        If mRenderControl IsNot Nothing Then
            If clearScreen OrElse charSizeCache.Count = 0 Then
                SyncLock MyBase.lockObject
                    charSizeCache.Clear()
                    Using g = mRenderControl.CreateGraphics()
                        For i As Integer = 0 To 255
                            MeasureChar(g, i, chars(i), mFont)
                        Next
                    End Using
                End SyncLock
            End If

            SyncLock MyBase.lockObject
                If videoBMP IsNot Nothing Then videoBMP.Dispose()
                Select Case MainMode
                    Case MainModes.Text
                        videoBMP = New DirectBitmap(640, 400)
                    Case MainModes.Graphics
                        videoBMP = New DirectBitmap(GraphicsResolution.Width, GraphicsResolution.Height)
                End Select
            End SyncLock

            ' Monospace... duh!
            charSize = charSizeCache(65)

            UpdateSystemInformationArea()
        End If
    End Sub
End Class
