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

    <Extension()>
    Public Function ToHex(value As UInteger, size As X8086.DataSize) As String
        If size = X8086.DataSize.Byte Then
            Return value.ToString("X2")
        Else
            Return value.ToString("X4")
        End If
    End Function
End Module
