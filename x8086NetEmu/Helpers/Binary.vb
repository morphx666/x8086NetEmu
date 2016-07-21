Public Class Binary
    Public Enum Sizes
        Bit = 1
        Nibble = 4
        [Byte] = 8
        Word = 16
        DoubleWord = 32
        QuadWord = 64
        'DoubleQuadWord = 128
        Undefined = -1
    End Enum

    Private binaryValue As Long
    Private mSize As Sizes

    Public Sub New()
        mSize = Sizes.Word
    End Sub

    Public Sub New(value As Long, Optional size As Sizes = Sizes.Undefined)
        Me.New()
        binaryValue = Math.Abs(value)
        If size = Sizes.Undefined Then
            CalculateMinimumSize()
        Else
            mSize = size
            binaryValue = binaryValue And Mask(size)
        End If
    End Sub

    Public Sub New(value As String, Optional size As Sizes = Sizes.Undefined)
        Dim binValue As Binary = 0
        TryParse(value, binValue)
        binaryValue = binValue

        If size = Sizes.Undefined Then
            mSize = binValue.Size
        Else
            mSize = Sizes.Word
        End If
    End Sub

    Public Shared Function TryParse(value As String, ByRef result As Long) As Boolean
        Try
            Select Case value.Last()
                Case "d"
                    result = Long.Parse(value.TrimEnd("d"))
                    Return True
                Case "h"
                    result = Convert.ToInt32(value.TrimEnd("h"), 16)
                    Return True
                Case "b"
                    result = Convert.ToInt32(value.TrimEnd("b"), 2)
                    Return True
                Case "o"
                    result = Convert.ToInt32(value.TrimEnd("o"), 8)
                    Return True
                Case Else
                    Dim base As Integer = 2
                    For Each c As Char In value
                        If c <> "0" AndAlso c <> "1" Then
                            If c >= "A" AndAlso c <= "F" Then
                                base = 16
                            ElseIf c < "0" OrElse c > "F" Then
                                base = -1
                                Exit For
                            ElseIf base <> 16 Then
                                base = 10
                            End If
                        End If
                    Next

                    If base = -1 Then
                        Return False
                    Else
                        result = Convert.ToInt32(value, base)
                        Return True
                    End If
            End Select
        Catch
            Return False
        End Try
    End Function

    Public Property Size As Sizes
        Get
            Return mSize
        End Get
        Set(value As Sizes)
            mSize = value
        End Set
    End Property

    Public Shared Narrowing Operator CType(value As Long) As Binary
        Return New Binary(value)
    End Operator

    Public Shared Narrowing Operator CType(value As Int32) As Binary
        Return New Binary(CUInt(Math.Abs(value)))
    End Operator

    Public Shared Narrowing Operator CType(value As String) As Binary
        Dim result As Binary = 0
        Binary.TryParse(value, result)
        Return result
    End Operator

    Public Shared Widening Operator CType(value As Binary) As Long
        Return value.ToLong()
    End Operator

    Public Shared Operator =(value1 As Binary, value2 As Binary) As Boolean
        Return value1.ToLong() = value2.ToLong()
    End Operator

    Public Shared Operator <>(value1 As Binary, value2 As Binary) As Boolean
        Return Not value1 = value2
    End Operator

    Public Shared Operator >(value1 As Binary, value2 As Binary) As Boolean
        Return value1.ToLong() > value2.ToLong()
    End Operator

    Public Shared Operator <(value1 As Binary, value2 As Binary) As Boolean
        Return Not value1 > value2
    End Operator

    Public Shared Operator >=(value1 As Binary, value2 As Binary) As Boolean
        Return value1.ToLong() >= value2.ToLong()
    End Operator

    Public Shared Operator <=(value1 As Binary, value2 As Binary) As Boolean
        Return Not value1 >= value2
    End Operator

    Public Shared Operator +(value1 As Binary, value2 As Binary) As Binary
        Return AdjustSize(value1.ToLong() + value2.ToLong(), value1.Size)
    End Operator

    Public Shared Operator -(value1 As Binary, value2 As Binary) As Binary
        Return AdjustSize(value1.ToLong() - value2.ToLong(), value1.Size)
    End Operator

    Public Shared Operator *(value1 As Binary, value2 As Binary) As Binary
        Return AdjustSize(value1.ToLong() * value2.ToLong(), value1.Size)
    End Operator

    Public Shared Operator /(value1 As Binary, value2 As Binary) As Binary
        Return AdjustSize(value1.ToLong() \ value2.ToLong(), value1.Size)
    End Operator

    Public Shared Operator \(value1 As Binary, value2 As Binary) As Binary
        Return value1 / value2
    End Operator

    Public Shared Operator ^(value1 As Binary, value2 As Binary) As Binary
        Return AdjustSize(value1.ToLong() ^ value2.ToLong(), value1.Size)
    End Operator

    Public Shared Operator Mod(value1 As Binary, value2 As Binary) As Binary
        Return AdjustSize(value1.ToLong() Mod value2.ToLong(), value1.Size)
    End Operator

    Public Shared Operator And(value1 As Binary, value2 As Binary) As Binary
        Return value1.ToLong() And value2.ToLong()
    End Operator

    Public Shared Operator Or(value1 As Binary, value2 As Binary) As Binary
        Return value1.ToLong() Or value2.ToLong()
    End Operator

    Public Shared Operator Xor(value1 As Binary, value2 As Binary) As Binary
        Return value1.ToLong() Xor value2.ToLong()
    End Operator

    Public Shared Operator Not(value1 As Binary) As Binary
        Return AdjustSize(Not value1.ToLong(), value1.Size)
    End Operator

    Public Shared Operator <<(value1 As Binary, value2 As Integer) As Binary
        Return AdjustSize(value1.ToLong() << value2, value1.Size)
    End Operator

    Public Shared Operator >>(value1 As Binary, value2 As Integer) As Binary
        Return AdjustSize(value1.ToLong() >> value2, value1.Size)
    End Operator

    Public Shared Function From(value As String, Optional size As Sizes = Sizes.Undefined) As Binary
        Return New Binary(value, size)
    End Function

    Public Shared Function From(value As Long, Optional size As Sizes = Sizes.Undefined) As Binary
        Return New Binary(value, size)
    End Function

    Public Shared Function From(value As Int32, Optional size As Sizes = Sizes.Undefined) As Binary
        Return Binary.From(CUInt(Math.Abs(value)), size)
    End Function

    Private Shared Function AdjustSize(value As Long, size As Sizes) As Binary
        Return New Binary(value And Mask(size), size)
    End Function

    Private Shared Function Mask(size As Sizes) As Long
        Return (2 ^ size) - 1
    End Function

    Public Function ToLong() As Long
        Return binaryValue
    End Function

    Public Overrides Function ToString() As String
        Return ConvertToBase(2).PadLeft(mSize, "0")
    End Function

    Public Function ToHex() As String
        Return ConvertToBase(16)
    End Function

    Public Function ToOctal() As String
        Return ConvertToBase(8)
    End Function

    Private Sub CalculateMinimumSize()
        If binaryValue <= 2 ^ 8 Then
            mSize = Sizes.Byte
        ElseIf binaryValue <= 2 ^ 16 Then
            mSize = Sizes.Word
        ElseIf binaryValue <= 2 ^ 32 Then
            mSize = Sizes.DoubleWord
        ElseIf binaryValue <= 2 ^ 64 Then
            mSize = Sizes.QuadWord
        Else
            Throw New OverflowException()
        End If
    End Sub

    Private Function ConvertToBase(base As Short) As String
        If mSize <= Sizes.DoubleWord Then
            Return Convert.ToString(CType(binaryValue, Integer), base).ToUpper()
        Else
            If base = 10 Then
                Return binaryValue
            Else
                Dim result As String = ""

                Dim i As Long
                Dim r As Long
                Dim n As Long = binaryValue
                Do
                    i = n \ base
                    r = n - i * base
                    result = Convert.ToString(r, base) + result
                    n = i
                Loop While n > 0

                Return result
            End If
        End If
    End Function
End Class