Public Class Image2Ascii
    Public Enum ColorModes
        GrayScale
        FullGrayScale
        Color
        DitheredGrayScale
        DitheredColor
    End Enum

    Public Enum ScanModes
        Fast
        Accurate
    End Enum

    Public Enum Charsets
        Standard = 0
        Advanced = 1
    End Enum

    Public Enum GrayscaleModes
        Average
        Accuarte
    End Enum

    Public Structure ASCIIChar
        Public Property Character As Char
        Public Property Color As Color

        Public Sub New(character As Char, color As Color)
            Me.Character = character
            Me.Color = color
        End Sub
    End Structure

    Private mBitmap As DirectBitmap
    Private mSurface As Bitmap

    Private mCanvasSize As Size
    Private mCanvas()() As ASCIIChar
    Private mColorMode As ColorModes
    Private mScanMode As ScanModes
    Private mCharset As Charsets
    Private mGrayScaleMode As GrayscaleModes
    Private mBackColor As Color

    Private mDitherColors As Integer = 8

    Private mFont As Font

    Private lastCanvasSize As Size = New Size(-1, -1)
    Private surfaceGraphics As Graphics
    Private charsetsChars() As String = {" ·:+x#W@", " ░░▒▒▓▓█"}
    Private activeChars As String = charsetsChars(0)

    Private mChatOffset As Point
    Private mCharSize As Size

    Private Shared c2ccCache As New Dictionary(Of Color, ConsoleColor)

    Public Event ImageProcessed(sender As Object, e As EventArgs)

    Public Sub New()
        mCanvasSize = New Size(80, 25)
        mColorMode = ColorModes.GrayScale
        mScanMode = ScanModes.Fast
        mCharset = Charsets.Standard
        mGrayScaleMode = GrayscaleModes.Average
        mBackColor = Color.Black
        mFont = New Font("Consolas", 12, GraphicsUnit.Pixel)
        SetCharSize()
    End Sub

    Public Property CanvasSize() As Size
        Get
            Return mCanvasSize
        End Get
        Set(value As Size)
            If mCanvasSize <> value Then
                mCanvasSize = value
                ProcessImage()
            End If
        End Set
    End Property

    Public ReadOnly Property DirectBitmap() As DirectBitmap
        Get
            Return mBitmap
        End Get
    End Property

    Public Property Bitmap() As Bitmap
        Get
            Return mBitmap
        End Get
        Set(value As Bitmap)
            mBitmap = value
            ProcessImage()
        End Set
    End Property

    Public ReadOnly Property Surface() As Bitmap
        Get
            Return mSurface
        End Get
    End Property

    Public Property GrayScaleMode() As GrayscaleModes
        Get
            Return mGrayScaleMode
        End Get
        Set(value As GrayscaleModes)
            mGrayScaleMode = value
            ProcessImage()
        End Set
    End Property

    Public Property Charset() As Charsets
        Get
            Return mCharset
        End Get
        Set(value As Charsets)
            mCharset = value
            activeChars = charsetsChars(mCharset)
            ProcessImage()
        End Set
    End Property

    Public Property ColorMode() As ColorModes
        Get
            Return mColorMode
        End Get
        Set(value As ColorModes)
            mColorMode = value
            ProcessImage()
        End Set
    End Property

    Public Property ScanMode() As ScanModes
        Get
            Return mScanMode
        End Get
        Set(value As ScanModes)
            mScanMode = value
            ProcessImage()
        End Set
    End Property

    Public ReadOnly Property CharSize() As Size
        Get
            Return mCharSize
        End Get
    End Property

    Public Property BackColor() As Color
        Get
            Return mBackColor
        End Get
        Set(value As Color)
            mBackColor = value
            ProcessImage()
        End Set
    End Property

    Public Property Font() As Font
        Get
            Return mFont
        End Get
        Set(value As Font)
            mFont = value
            SetCharSize()
            ProcessImage()
        End Set
    End Property

    Public Property DitherColors As Integer
        Get
            Return mDitherColors
        End Get
        Set(value As Integer)
            If value >= 2 Then
                mDitherColors = value
                ProcessImage()
            Else
                ' Throw New ArgumentOutOfRangeException($"{NameOf(DitherColors)} must be 2 or larger")
            End If
        End Set
    End Property

    Public ReadOnly Property Canvas() As ASCIIChar()()
        Get
            Return mCanvas
        End Get
    End Property

    Private Sub SetCharSize()
        Dim IsBlack = Function(c As Color) As Boolean
                          Return c.R = 0 AndAlso c.G = 0 AndAlso c.B = 0
                      End Function

        Using bmp As New DirectBitmap(100, 100)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
                g.PixelOffsetMode = Drawing2D.PixelOffsetMode.None
                g.SmoothingMode = Drawing2D.SmoothingMode.None

                g.Clear(Color.Black)
                g.DrawString("█", mFont, Brushes.White, 0, 0)
            End Using

            Dim lt As Point
            Dim rb As Point

            For y As Integer = 0 To bmp.Height - 1
                For x As Integer = 0 To bmp.Width - 1
                    If Not IsBlack(bmp.Pixel(x, y)) Then
                        lt = New Point(x, y)
                        y = bmp.Height
                        Exit For
                    End If
                Next
            Next

            For y As Integer = bmp.Height - 1 To 0 Step -1
                For x As Integer = bmp.Width - 1 To 0 Step -1
                    If Not IsBlack(bmp.Pixel(x, y)) Then
                        rb = New Point(x, y)
                        y = 0
                        Exit For
                    End If
                Next
            Next

            mCharSize = New Size(rb.X - lt.X, rb.Y - lt.Y)
            If mCharSize.Width > 1 Then mCharSize.Width -= 1
        End Using
    End Sub

    Public Sub ProcessImage(Optional surfaceGraphics As Boolean = True)
        If mBitmap Is Nothing Then Exit Sub

        Dim sx As Integer
        Dim sy As Integer

        If lastCanvasSize <> mCanvasSize Then
            lastCanvasSize = mCanvasSize

            If mSurface IsNot Nothing Then mSurface.Dispose()
            mSurface = New DirectBitmap(mCanvasSize.Width * CharSize.Width, mCanvasSize.Height * CharSize.Height)

            ReDim mCanvas(mCanvasSize.Width - 1)
            For x = 0 To mCanvasSize.Width - 1
                ReDim mCanvas(x)(mCanvasSize.Height - 1)
                For y = 0 To mCanvasSize.Height - 1
                    mCanvas(x)(y) = New ASCIIChar(" ", Me.BackColor)
                Next
            Next
        End If

        If surfaceGraphics Then
            Me.surfaceGraphics = Graphics.FromImage(mSurface)
            Me.surfaceGraphics.Clear(Me.BackColor)
        End If

        Dim scanStep As Size = New Size(Math.Ceiling(mBitmap.Width / mCanvasSize.Width), Math.Ceiling(mBitmap.Height / mCanvasSize.Height))
        'scanStep.Width += mCanvasSize.Width Mod scanStep.Width
        'scanStep.Height += mCanvasSize.Height Mod scanStep.Height
        Dim scanStepSize = scanStep.Width * scanStep.Height

        ' Source color
        Dim r As Integer
        Dim g As Integer
        Dim b As Integer

        ' Dithered Color
        Dim dr As Integer
        Dim dg As Integer
        Dim db As Integer
        Dim dColorFactor As Integer = mDitherColors - 1
        Dim dFactor As Double = 255 / dColorFactor
        Dim quantaError(3 - 1) As Double
        Dim ApplyQuantaError = Sub(qx As Integer, qy As Integer, qr As Integer, qg As Integer, qb As Integer, w As Double)
                                   If qx < 0 OrElse qx >= mCanvasSize.Width OrElse
                                       qy < 0 OrElse qy >= mCanvasSize.Height Then Exit Sub
                                   qr += quantaError(0) * w
                                   qg += quantaError(1) * w
                                   qb += quantaError(2) * w
                                   mCanvas(qx)(qy) = New ASCIIChar(ColorToASCII(qr, qg, qb), Color.FromArgb(qr, qg, qb))
                               End Sub

        ' For gray scale modes
        Dim gray As Integer

        Dim offset As Integer

        For y As Integer = 0 To mBitmap.Height - scanStep.Height - 1 Step scanStep.Height
            For x As Integer = 0 To mBitmap.Width - scanStep.Width - 1 Step scanStep.Width
                If mScanMode = ScanModes.Fast Then
                    offset = (x + y * mBitmap.Width) * 4
                    r = mBitmap.Bits(offset + 2)
                    g = mBitmap.Bits(offset + 1)
                    b = mBitmap.Bits(offset + 0)
                Else
                    r = 0
                    g = 0
                    b = 0

                    For y1 = y To y + scanStep.Height - 1
                        For x1 = x To x + scanStep.Width - 1
                            offset = (x1 + y1 * mBitmap.Width) * 4

                            r += mBitmap.Bits(offset + 2)
                            g += mBitmap.Bits(offset + 1)
                            b += mBitmap.Bits(offset + 0)
                        Next
                    Next

                    r /= scanStepSize
                    g /= scanStepSize
                    b /= scanStepSize
                End If

                sx = x / scanStep.Width
                sy = y / scanStep.Height

                Select Case mColorMode
                    Case ColorModes.GrayScale
                        mCanvas(sx)(sy) = New ASCIIChar(ColorToASCII(r, g, b), Color.White)
                    Case ColorModes.FullGrayScale
                        gray = ToGrayScale(r, g, b)
                        mCanvas(sx)(sy) = New ASCIIChar(ColorToASCII(r, g, b), Color.FromArgb(gray, gray, gray))
                    Case ColorModes.Color
                        mCanvas(sx)(sy) = New ASCIIChar(ColorToASCII(r, g, b), Color.FromArgb(r, g, b))
                    Case ColorModes.DitheredGrayScale, ColorModes.DitheredColor
                        ' https://en.wikipedia.org/wiki/Floyd%E2%80%93Steinberg_dithering
                        If mColorMode = ColorModes.DitheredGrayScale Then
                            r = ToGrayScale(r, g, b)
                            g = r
                            b = r
                        End If
                        dr = Math.Round(dColorFactor * r / 255) * dFactor
                        dg = Math.Round(dColorFactor * g / 255) * dFactor
                        db = Math.Round(dColorFactor * b / 255) * dFactor

                        mCanvas(sx)(sy) = New ASCIIChar(ColorToASCII(dr, dg, db), Color.FromArgb(dr, dg, db))

                        quantaError = {Math.Max(0, r - dr),
                                       Math.Max(0, g - dg),
                                       Math.Max(0, b - db)}

                        ApplyQuantaError(sx + 1, sy, dr, dg, db, 7 / 16)
                        ApplyQuantaError(sx - 1, sy + 1, dr, dg, db, 3 / 16)
                        ApplyQuantaError(sx, sy + 1, dr, dg, db, 5 / 16)
                        ApplyQuantaError(sx + 1, sy + 1, dr, dg, db, 1 / 16)
                End Select

                If surfaceGraphics Then
                    Using sb As New SolidBrush(mCanvas(sx)(sy).Color)
                        Me.surfaceGraphics.DrawString(mCanvas(sx)(sy).Character, Me.Font, sb, sx * CharSize.Width, sy * CharSize.Height)
                    End Using
                End If
            Next
        Next
        If surfaceGraphics Then Me.surfaceGraphics.Dispose()

        RaiseEvent ImageProcessed(Me, New EventArgs())
    End Sub

    Private Function ColorToASCII(color As Color) As Char
        Return ColorToASCII(color.R, color.G, color.B)
    End Function

    Private Function ColorToASCII(r As Integer, g As Integer, b As Integer) As Char
        Return activeChars(Math.Floor(ToGrayScale(r, g, b) / (256 / activeChars.Length)))
    End Function

    Private Function ToGrayScale(r As Integer, g As Integer, b As Integer) As Double
        Select Case mGrayScaleMode
            Case GrayscaleModes.Accuarte
                Return r * 0.2126 + g * 0.7152 + b * 0.0722
            Case GrayscaleModes.Average
                Return r / 3 + g / 3 + b / 3
            Case Else
                Return 0
        End Select
    End Function

    Public Shared Function ToConsoleColor(c As Color) As ConsoleColor
        Dim d As Double
        Dim minD As Double = Double.MaxValue
        Dim bestResult As ConsoleColor
        Dim ccRgb() As Integer = Nothing

        If c2ccCache.ContainsKey(c) Then Return c2ccCache(c)

        For Each cc As ConsoleColor In [Enum].GetValues(GetType(ConsoleColor))
            Select Case cc
                Case ConsoleColor.Black : ccRgb = HexColorToArray("000000")
                Case ConsoleColor.DarkBlue : ccRgb = HexColorToArray("000080")
                Case ConsoleColor.DarkGreen : ccRgb = HexColorToArray("008000")
                Case ConsoleColor.DarkCyan : ccRgb = HexColorToArray("008080")
                Case ConsoleColor.DarkRed : ccRgb = HexColorToArray("800000")
                Case ConsoleColor.DarkMagenta : ccRgb = HexColorToArray("800080")
                Case ConsoleColor.DarkYellow : ccRgb = HexColorToArray("808000")
                Case ConsoleColor.Gray : ccRgb = HexColorToArray("C0C0C0")
                Case ConsoleColor.DarkGray : ccRgb = HexColorToArray("808080")
                Case ConsoleColor.Blue : ccRgb = HexColorToArray("0000FF")
                Case ConsoleColor.Green : ccRgb = HexColorToArray("00FF00")
                Case ConsoleColor.Cyan : ccRgb = HexColorToArray("00FFFF")
                Case ConsoleColor.Red : ccRgb = HexColorToArray("FF0000")
                Case ConsoleColor.Magenta : ccRgb = HexColorToArray("FF00FF")
                Case ConsoleColor.Yellow : ccRgb = HexColorToArray("FFFF00")
                Case ConsoleColor.White : ccRgb = HexColorToArray("FFFFFF")
            End Select

            d = Math.Sqrt((c.R - ccRgb(0)) ^ 2 + (c.G - ccRgb(1)) ^ 2 + (c.B - ccRgb(2)) ^ 2)
            If d < minD Then
                minD = d
                bestResult = cc
            End If
        Next

        c2ccCache.Add(c, bestResult)
        Return bestResult
    End Function

    ' EGA Palette
    ' http://stackoverflow.com/questions/1988833/converting-color-to-consolecolor
    Public Shared Function ToConsoleColorEGA(c As Color) As ConsoleColor
        Dim index As Integer = If(c.R > 128 Or c.G > 128 Or c.B > 128, 8, 0) ' Bright bit
        index = index Or If(c.R > 64, 4, 0) ' Red bit
        index = index Or If(c.G > 64, 2, 0) ' Green bit
        index = index Or If(c.B > 64, 1, 0) ' Blue bit
        Return index
    End Function

    Public Shared Function HexColorToArray(hexColor As String) As Integer()
        Return {Integer.Parse(hexColor.Substring(0, 2), Globalization.NumberStyles.HexNumber),
                Integer.Parse(hexColor.Substring(2, 2), Globalization.NumberStyles.HexNumber),
                Integer.Parse(hexColor.Substring(4, 2), Globalization.NumberStyles.HexNumber)}
    End Function
End Class
