Imports System.Runtime.CompilerServices
Imports x8086NetEmu

Module Extensions
    <Extension()>
    Public Function LowByte(value As Integer) As Byte
        Return value And &HFF
    End Function

    <Extension()>
    Public Function HighByte(value As Integer) As Byte
        Return value >> 8
    End Function

    <Extension()>
    Public Function LowNib(value As Byte) As Byte
        Return value And &HF
    End Function

    <Extension()>
    Public Function HighNib(value As Byte) As Byte
        Return value >> 4
    End Function

    <Extension()>
    Public Function ToBinary(value As Byte) As String
        Return Convert.ToString(value, 2).PadLeft(8, "0")
    End Function

    <Extension()>
    Public Function ToBinary(value As Integer) As String
        Return Convert.ToString(value, 2).PadLeft(16, "0")
    End Function

    <Extension()>
    Public Function ToBinary(value As X8086.GPRegisters.RegistersTypes) As String
        Return Convert.ToString(value, 2)
    End Function

    <Extension()>
    Public Function ToHex(value As Byte, Optional suffix As String = "h") As String
        Return value.ToString("X2") + suffix
    End Function

    <Extension()>
    Public Function ToHex(value As Integer, size As X8086.DataSize, Optional suffix As String = "h") As String
        If size = X8086.DataSize.Byte Then
            Return value.ToString("X2") + suffix
        Else
            Return value.ToString("X4") + suffix
        End If
    End Function

    <Extension()>
    Public Function ToHex(value As UInteger, size As X8086.DataSize, Optional suffix As String = "h") As String
        If size = X8086.DataSize.Byte Then
            Return value.ToString("X2") + suffix
        Else
            Return value.ToString("X4") + suffix
        End If
    End Function

    <Extension()>
    Public Function ToHex(value As Short, size As X8086.DataSize, Optional suffix As String = "h") As String
        If size = X8086.DataSize.Byte Then
            Return value.ToString("X2") + suffix
        Else
            Return value.ToString("X4") + suffix
        End If
    End Function

    <Extension()>
    Public Function ToHex(value As UShort, size As X8086.DataSize, Optional suffix As String = "h") As String
        If size = X8086.DataSize.Byte Then
            Return value.ToString("X2") + suffix
        Else
            Return value.ToString("X4") + suffix
        End If
    End Function

    <Extension()>
    Public Function ToHex(value As Long, size As X8086.DataSize, Optional suffix As String = "h") As String
        Select Case size
            Case X8086.DataSize.Byte
                Return value.ToString("X2") + suffix
            Case X8086.DataSize.Word
                Return value.ToString("X4") + suffix
            Case X8086.DataSize.DWord
                Return value.ToString("X8") + suffix
            Case Else
                Return ""
        End Select
    End Function

    <Extension()>
    Public Function ToHex(value As ULong, size As X8086.DataSize, Optional suffix As String = "h") As String
        Select Case size
            Case X8086.DataSize.Byte
                Return value.ToString("X2") + suffix
            Case X8086.DataSize.Word
                Return value.ToString("X4") + suffix
            Case X8086.DataSize.DWord
                Return value.ToString("X8") + suffix
            Case Else
                Return ""
        End Select
    End Function

    <Extension()>
    Public Function ToBCD(value As Integer) As Integer
        Dim v As Integer
        Dim r As Integer

        For i As Integer = 0 To 4 - 1
            v = value Mod 10
            value /= 10
            v = v Or ((value Mod 10) << 4)
            value /= 10

            r += v << (4 * i)
        Next

        Return r
    End Function
End Module
