Imports System.Drawing.Imaging
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

' Credit: SaxxonPike (http://stackoverflow.com/users/3117338/saxxonpike)
' http://stackoverflow.com/questions/24701703/c-sharp-faster-alternatives-to-setpixel-And-getpixel-for-bitmaps-for-windows-f

Public Class DirectBitmap
    Implements IDisposable

    Public ReadOnly Property Bitmap As Bitmap
    Public ReadOnly Property Width As Integer
    Public ReadOnly Property Height As Integer
    Public ReadOnly Property Bits As Byte()

    Private bitsHandle As GCHandle
    Private w4 As Integer
    Private bufferSize As Integer

    Public Sub New(w As Integer, h As Integer)
        Me.Width = w
        Me.Height = h

        w4 = w * 4
        bufferSize = w4 * h - 1
        ReDim Bits(bufferSize)

        bitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned)
        Me.Bitmap = New Bitmap(w, h, w4, PixelFormat.Format32bppPArgb, bitsHandle.AddrOfPinnedObject())
    End Sub

    Public Sub New(bmp As Bitmap)
        Me.New(bmp.Width, bmp.Height)

        Dim sourceData As BitmapData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat)
        Dim sourcePointer As IntPtr = sourceData.Scan0
        Dim sourceStride As Integer = sourceData.Stride
        Dim srcBytesPerPixel As Integer = sourceStride \ bmp.Width
        Dim srcOffset As Integer

        Dim a As Integer
        Dim pa As Double

        For y As Integer = 0 To bmp.Height - 1
            For x As Integer = 0 To bmp.Width - 1
                srcOffset = x * srcBytesPerPixel + y * sourceStride

                a = If(srcBytesPerPixel = 4, Marshal.ReadByte(sourcePointer, srcOffset + 3), 255)
                pa = a / 255
                Pixel(x, y) = Color.FromArgb(a,
                                             Marshal.ReadByte(sourcePointer, srcOffset + 2) * pa,
                                             Marshal.ReadByte(sourcePointer, srcOffset + 1) * pa,
                                             Marshal.ReadByte(sourcePointer, srcOffset + 0) * pa)
            Next
        Next

        bmp.UnlockBits(sourceData)
    End Sub

    Public Shared Widening Operator CType(bmp As Bitmap) As DirectBitmap
        If bmp Is Nothing Then Return Nothing

        Dim dbmp As New DirectBitmap(bmp.Width, bmp.Height)
        Using g As Graphics = Graphics.FromImage(dbmp.Bitmap)
            g.DrawImageUnscaled(bmp, Point.Empty)
        End Using

        Return dbmp
    End Operator

    Public Shared Narrowing Operator CType(dbmp As DirectBitmap) As Bitmap
        If dbmp Is Nothing Then Return Nothing
        Return dbmp.Bitmap
    End Operator

    Public Property Pixel(x As Integer, y As Integer) As Color
        Get
            If x < 0 OrElse x >= Width OrElse y < 0 OrElse y >= Height Then Exit Property
            Dim offset As Integer = y * w4 + x * 4
            Return Color.FromArgb(Bits(offset + 3),
                                  Bits(offset + 2),
                                  Bits(offset + 1),
                                  Bits(offset + 0))
        End Get
        Set(value As Color)
            If x < 0 OrElse x >= Width OrElse y < 0 OrElse y >= Height Then Exit Property
            Dim offset As Integer = y * w4 + x * 4
            Bits(offset + 3) = value.A
            Bits(offset + 2) = value.R
            Bits(offset + 1) = value.G
            Bits(offset + 0) = value.B
        End Set
    End Property

    Public Property Pixel2(x As Integer, y As Integer) As Color
        Get
            If x < 0 OrElse x >= Width OrElse y < 0 OrElse y >= Height Then Exit Property
            Dim offset As Integer = y * w4 + x * 4
            Return Color.FromArgb(Bits(offset + 3),
                                  Bits(offset + 2),
                                  Bits(offset + 1),
                                  Bits(offset + 0))
        End Get
        Set(value As Color)
            If x < 0 OrElse x >= Width OrElse y < 0 OrElse y >= Height Then Exit Property
            Dim offset As Integer = y * w4 + x * 4

            Dim A As Integer = Bits(offset + 3)
            Dim R As Integer = Bits(offset + 2)
            Dim G As Integer = Bits(offset + 1)
            Dim B As Integer = Bits(offset + 0)

            Dim A1p As Double = value.A / 256.0
            Dim A2p As Double = A / 256.0
            Dim OA As Double = GetAlpha(A1p, A2p)

            Bits(offset + 3) = Int(OA * 255)
            Bits(offset + 2) = ApplyAlpha(value.R, R, A1p, A2p, OA)
            Bits(offset + 1) = ApplyAlpha(value.G, G, A1p, A2p, OA)
            Bits(offset + 0) = ApplyAlpha(value.B, B, A1p, A2p, OA)
        End Set
    End Property

    Private Function ApplyAlpha(c1 As Integer, c2 As Integer, a1p As Double, a2p As Double, a As Double) As Integer
        If a = 0 Then Return 0
        Return Int((c1 * a1p + c2 * a2p * (1 - a1p)) * a)
    End Function

    Private Function GetAlpha(a1p As Double, a2p As Double) As Double
        Return (a1p + a2p * (1 - a1p))
    End Function

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
                Bitmap.Dispose()
                bitsHandle.Free()
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region
End Class

Module DirectBitmapExtensions
    Private Const ToRad As Double = Math.PI / 180.0
    Private Const ToDeg As Double = 180.0 / Math.PI

    <Extension>
    Public Sub Clear(dbmp As DirectBitmap, c As Color)
        Dim b() = {c.B, c.G, c.R, c.A}
        Dim bufferSize As Integer = dbmp.Height * dbmp.Width * 4 - 1
        For i As Integer = 0 To bufferSize Step 4
            Array.Copy(b, 0, dbmp.Bits, i, 4)
        Next
    End Sub

    <Extension()>
    Public Sub DrawLine(dbmp As DirectBitmap, c As Color, x1 As Integer, y1 As Integer, x2 As Integer, y2 As Integer)
        Dim dx As Integer = x2 - x1
        Dim dy As Integer = y2 - y1
        Dim l As Integer = Math.Sqrt(dx ^ 2 + dy ^ 2)
        Dim a As Double = Math.Atan2(dy, dx)
        For r As Integer = 0 To l
            dbmp.Pixel(x1 + r * Math.Cos(-a), y1 + r * Math.Sin(a)) = c
        Next
    End Sub

    <Extension()>
    Public Sub DrawLine(dbmp As DirectBitmap, c As Color, p1 As Point, p2 As Point)
        dbmp.DrawLine(c, p1.X, p1.Y, p2.X, p2.Y)
    End Sub

    <Extension()>
    Public Sub DrawPolygon(dbmp As DirectBitmap, c As Color, p() As Point)
        Dim j As Integer
        Dim l As Integer = p.Length
        For i As Integer = 0 To l - 1
            j = (i + 1) Mod l
            dbmp.DrawLine(c, p(i), p(j))
        Next
    End Sub

    <Extension()>
    Public Sub DrawPolygon(dbmp As DirectBitmap, c As Color, p() As PointF)
        Dim l As Integer = p.Length - 1
        Dim pi(l) As Point
        For i As Integer = 0 To l
            pi(i) = New Point(p(i).X, p(i).Y)
        Next
        dbmp.DrawPolygon(c, pi)
    End Sub

    <Extension()>
    Public Sub FillRectangle(dbmp As DirectBitmap, c As Color, r As Rectangle)
        FillRectangle(dbmp, c, r.X, r.Y, r.Width, r.Height)
    End Sub

    <Extension()>
    Public Sub FillRectangle(dbmp As DirectBitmap, c As Color, x As Integer, y As Integer, w As Integer, h As Integer)
        For x1 As Integer = x To x + w - 1
            For y1 As Integer = y To y + h - 1
                dbmp.Pixel(x1, y1) = c
            Next
        Next
    End Sub
End Module
