Partial Public Class x8086
    Private Const Size As Integer = 6

    Private mBuffer(Size - 1) As Byte

    Private mAddress As Integer

    Public Sub Prefetch()
        mAddress = SegOffToAbs(mRegisters.CS, mRegisters.IP)

        For i As Integer = 0 To Size - 1
            mBuffer(i) = Memory((mAddress + i) Mod MemSize)
        Next
    End Sub

    Public ReadOnly Property Buffer As Byte()
        Get
            Return mBuffer
        End Get
    End Property

    Public ReadOnly Property FromPreftch(testAddress As Integer) As Byte
        Get
            testAddress = testAddress And &HFFFFF ' "Call 5" Legacy Interface: http://www.os2museum.com/wp/?p=734
            If testAddress >= mAddress AndAlso testAddress < mAddress + Size - 1 Then
                Return mBuffer(testAddress - mAddress)
            Else
                Return Memory(testAddress)
            End If
        End Get
    End Property
End Class
