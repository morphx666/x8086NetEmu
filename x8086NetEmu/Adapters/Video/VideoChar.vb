Public Class VideoChar
    Private mCGAChar As Integer
    Private mForeColor As Color
    Private mBackColor As Color
    Private mBitmap As DirectBitmap

    Public Shared FontBitmaps() As Byte

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

    Private tmp As New Object()

    Public Sub Paint(dbmp As DirectBitmap, p As Point, scale As SizeF)
        Dim w4s As Integer = mBitmap.Width * 4
        Dim w4d As Integer = dbmp.Width * 4
        p.X *= 4
        For y As Integer = 0 To mBitmap.Height - 1
            Array.Copy(mBitmap.Bits, y * w4s, dbmp.Bits, (y + p.Y) * w4d + p.X, w4s)
        Next
    End Sub

    Public Sub Render(w As Integer, h As Integer)
        If mBitmap Is Nothing Then
            w = 8
            h = 16
            mBitmap = New DirectBitmap(w, h)

            For y As Integer = 0 To h - 1
                For x As Integer = 0 To w - 1
                    If FontBitmaps(mCGAChar * (w * h) + y * w + x) = 1 Then
                        mBitmap.Pixel(x, y) = mForeColor
                    Else
                        mBitmap.Pixel(x, y) = mBackColor
                    End If
                Next
            Next
        End If
    End Sub

    Public Shared Operator =(c1 As VideoChar, c2 As VideoChar) As Boolean
        Return c1.CGAChar = c2.CGAChar AndAlso
                c1.ForeColor = c2.ForeColor AndAlso
                c1.BackColor = c2.BackColor
    End Operator

    Public Shared Operator <>(c1 As VideoChar, c2 As VideoChar) As Boolean
        Return Not (c1 = c2)
    End Operator

    Public Overrides Function Equals(obj As Object) As Boolean
        Return Me = CType(obj, VideoChar)
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

    ' http://goughlui.com/2016/05/01/project-examining-vga-bios-from-old-graphic-cards/
    Public Shared Sub BuildFontBitmapsFromROM(fontWidth As Integer, fontHeight As Integer, romFontHeight As Integer, romOffset As Integer, rom() As Byte)
        Dim fw As Integer = fontWidth
        Dim fh As Integer = fontHeight
        Dim dataW As Integer = 1
        Dim dataH As Integer = romFontHeight

        Dim romSize As Integer = rom.Length
        Dim offset As Integer = romOffset
        ReDim VideoChar.FontBitmaps(fw * fh * 512 - 1)

        Dim tempCount As Integer = 0
        Dim base As Integer = 0
        Dim row As Integer = 0
        Dim mask As Integer = &H80

        Dim x As Integer = 0
        Dim y As Integer = 0

        For i As Integer = 0 To 512 - 1
            While base < dataH
                While tempCount < dataW
                    While mask <> 0
                        If (rom((base + (tempCount * dataH) + (row * dataW * dataH) + offset) Mod romSize) And mask) <> 0 Then
                            VideoChar.FontBitmaps(i * (fw * fh) + y * fw + x) = 1
                        Else
                            VideoChar.FontBitmaps(i * (fw * fh) + y * fw + x) = 0
                        End If
                        x += 1
                        mask = mask >> 1
                    End While
                    tempCount += 1
                    mask = &H80
                End While
                tempCount = 0
                base += 1
                x = 0
                y += 1
            End While
            base = 0
            row += 1
            x = 0
            y = 0
        Next
    End Sub
End Class