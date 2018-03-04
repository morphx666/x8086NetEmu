'The MAIN Guide: http://docs.huihoo.com/help-pc/index.html
' http://www.delorie.com/djgpp/doc/rbinter/ix/

Partial Public Class X8086
    Private lastAH(256 - 1) As UInteger
    Private lastCF(256 - 1) As UShort

    Public Sub HandleHardwareInterrupt(intNum As Byte)
        HandleInterrupt(intNum, True)
        mRegisters.IP = IPAddrOff
    End Sub

    Private Sub HandlePendingInterrupt()
        ' Lesson 5 (mRegisters.ActiveSegmentChanged = False)
        ' http://ntsecurity.nu/onmymind/2007/2007-08-22.html

        If mFlags.IF = 1 AndAlso
           mFlags.TF = 0 AndAlso
           Not mRegisters.ActiveSegmentChanged AndAlso
           repeLoopMode = REPLoopModes.None AndAlso
           picIsAvailable Then

            Dim pendingIntNum As Integer = PIC.GetPendingInterrupt()
            If pendingIntNum >= 0 Then
                mIsHalted = False
                HandleHardwareInterrupt(pendingIntNum)
            End If
        End If
    End Sub

    Private Sub HandleInterrupt(intNum As Byte, isHard As Boolean)
        'FlushCycles()

        If Not (intHooks.ContainsKey(intNum) AndAlso intHooks(intNum).Invoke()) Then
            PushIntoStack(mFlags.EFlags)
            PushIntoStack(mRegisters.CS)

            If isHard Then
                PushIntoStack(mRegisters.IP)
            Else
                PushIntoStack(AddValues(mRegisters.IP, opCodeSize, DataSize.Word))
            End If

            Dim intOffset As UInteger = intNum * 4
            IPAddrOff = RAM16(0, intOffset)
            mRegisters.CS = RAM16(0, AddValues(intOffset, 2, DataSize.Word))

            If intNum = 0 Then ThrowException("Division by Zero")
        End If

        mFlags.IF = 0
        mFlags.TF = 0

        clkCyc += 51
    End Sub
End Class