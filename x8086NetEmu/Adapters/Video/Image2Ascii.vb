Public Class Image2Ascii
    Public Enum ColorModes
        GrayScale
        FullGrayScale
        Color
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

    Private mCanvasSize As Size = New Size(80, 25)
    Private mCanvas()() As ASCIIChar
    Private mColorMode As ColorModes = ColorModes.GrayScale
    Private mScanMode As ScanModes = ScanModes.Fast
    Private mCharset As Charsets = Charsets.Standard
    Private mGrayScaleMode As GrayscaleModes = GrayscaleModes.Average
    Private mBackColor As Color = Color.Black

    Private mFont As New Font("Consolas", 12)

    Private lastCanvasSize As Size = New Size(-1, -1)
    Private surfaceGraphics As Graphics
    Private charsetsChars() As String = {" ·:+x#W@", " ░░▒▒▓▓█"}
    Private activeChars As String = charsetsChars(0)

    Private mCharSize As Size

    Private Shared c2ccCache As New Dictionary(Of Color, ConsoleColor)

    Public Event ImageProcessed(sender As Object, e As EventArgs)

    Public Sub New()
        SetCharSize()
    End Sub

    Public Property CanvasSize As Size
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

    Public ReadOnly Property DirectBitmap As DirectBitmap
        Get
            Return mBitmap
        End Get
    End Property

    Public Property Bitmap As Bitmap
        Get
            Return mBitmap
        End Get
        Set(value As Bitmap)
            mBitmap = value
            ProcessImage()
        End Set
    End Property

    Public ReadOnly Property Surface As Bitmap
        Get
            Return mSurface
        End Get
    End Property

    Public Property GrayScaleMode As GrayscaleModes
        Get
            Return mGrayScaleMode
        End Get
        Set(value As GrayscaleModes)
            mGrayScaleMode = value
            ProcessImage()
        End Set
    End Property

    Public Property Charset As Charsets
        Get
            Return mCharset
        End Get
        Set(value As Charsets)
            mCharset = value
            activeChars = charsetsChars(mCharset)
            ProcessImage()
        End Set
    End Property

    Public Property ColorMode As ColorModes
        Get
            Return mColorMode
        End Get
        Set(value As ColorModes)
            mColorMode = value
            ProcessImage()
        End Set
    End Property

    Public Property ScanMode As ScanModes
        Get
            Return mScanMode
        End Get
        Set(value As ScanModes)
            mScanMode = value
            ProcessImage()
        End Set
    End Property

    Public ReadOnly Property CharSize As Size
        Get
            Return mCharSize
        End Get
    End Property

    Public Property BackColor As Color
        Get
            Return mBackColor
        End Get
        Set(value As Color)
            mBackColor = value
            ProcessImage()
        End Set
    End Property

    Public Property Font As Font
        Get
            Return mFont
        End Get
        Set(value As Font)
            mFont = Font
            SetCharSize()
            ProcessImage()
        End Set
    End Property

    Public ReadOnly Property Canvas As ASCIIChar()()
        Get
            Return mCanvas
        End Get
    End Property

    Private Sub SetCharSize()
        mCharSize = TextRenderer.MeasureText("X", mFont)
        mCharSize.Width -= 8
        mCharSize.Height -= 1
    End Sub

    Public Sub ProcessImage(Optional gurfaceGraphics As Boolean = True)
        If mBitmap Is Nothing Then Exit Sub

        Dim sx As Integer
        Dim sy As Integer

        Dim sizeChanged As Boolean = (lastCanvasSize <> mCanvasSize)

        If sizeChanged Then
            If mSurface IsNot Nothing Then mSurface.Dispose()
            mSurface = New DirectBitmap(mCanvasSize.Width * CharSize.Width, mCanvasSize.Height * CharSize.Height)
        End If

        If gurfaceGraphics Then
            surfaceGraphics = Graphics.FromImage(mSurface)
            surfaceGraphics.Clear(Me.BackColor)
        End If

        Dim scanStep As Size = New Size(Math.Ceiling(mBitmap.Width / mCanvasSize.Width), Math.Ceiling(mBitmap.Height / mCanvasSize.Height))
        'scanStep.Width += mCanvasSize.Width Mod scanStep.Width
        'scanStep.Height += mCanvasSize.Height Mod scanStep.Height
        Dim scanStepSize = scanStep.Width * scanStep.Height

        If sizeChanged Then ReDim mCanvas(mCanvasSize.Width - 1)

        For x = 0 To mCanvasSize.Width - 1
            If sizeChanged Then ReDim mCanvas(x)(mCanvasSize.Height - 1)
            For y = 0 To mCanvasSize.Height - 1
                mCanvas(x)(y) = New ASCIIChar(" ", Me.BackColor)
            Next
        Next

        Dim r As Integer
        Dim g As Integer
        Dim b As Integer
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
                End Select

                If gurfaceGraphics Then
                    Using sb As New SolidBrush(mCanvas(sx)(sy).Color)
                        surfaceGraphics.DrawString(mCanvas(sx)(sy).Character, Me.Font, sb, sx * CharSize.Width, sy * CharSize.Height)
                    End Using
                End If
            Next
        Next
        If gurfaceGraphics Then surfaceGraphics.Dispose()

        lastCanvasSize = mCanvasSize

        RaiseEvent ImageProcessed(Me, New EventArgs())
    End Sub

    Private Function ColorToASCII(color As Color) As Char
        Return ColorToASCII(color.R, color.G, color.B)
    End Function

    Private Function ToGrayScale(r As Integer, g As Integer, b As Integer) As Double
        Select Case mGrayScaleMode
            Case GrayscaleModes.Accuarte
                Return r * 0.2126 + g * 0.7152 + b * 0.0722
            Case GrayscaleModes.Average
                Return r * 1 / 3 + g * 1 / 3 + b * 1 / 3
            Case Else
                Return 0
        End Select
    End Function

    Private Function ColorToASCII(r As Integer, g As Integer, b As Integer) As Char
        Return activeChars(Math.Floor(ToGrayScale(r, g, b) / (256 / activeChars.Length)))
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
        Return CType(index, ConsoleColor)
    End Function

    Public Shared Function HexColorToArray(hexColor As String) As Integer()
        Return {Integer.Parse(hexColor.Substring(0, 2), Globalization.NumberStyles.HexNumber),
                Integer.Parse(hexColor.Substring(2, 2), Globalization.NumberStyles.HexNumber),
                Integer.Parse(hexColor.Substring(4, 2), Globalization.NumberStyles.HexNumber)}
    End Function
End Class
