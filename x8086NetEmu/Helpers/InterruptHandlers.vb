'The MAIN Guide: http://docs.huihoo.com/help-pc/index.html or http://stanislavs.org/helppc/ (mirror)
' http://www.delorie.com/djgpp/doc/rbinter/ix/

Partial Public Class X8086
    Private lastAH(256 - 1) As UInt16
    Private lastCF(256 - 1) As Byte

    Public Sub HandleHardwareInterrupt(intNum As Byte)
        HandleInterrupt(intNum, True)
        mRegisters.IP = IPAddrOffet
    End Sub

    Private Sub HandlePendingInterrupt()
        ' Lesson 5 (mRegisters.ActiveSegmentChanged = False)
        ' http://ntsecurity.nu/onmymind/2007/2007-08-22.html

        If mFlags.IF = 1 AndAlso
           mFlags.TF = 0 AndAlso
           Not mRegisters.ActiveSegmentChanged AndAlso
           Not newPrefix AndAlso
           picIsAvailable Then

            Dim pendingIntNum As Byte = PIC.GetPendingInterrupt()
            If pendingIntNum <> &HFF Then
                If mIsHalted Then
                    mIsHalted = False
                    ' https://docs.oracle.com/cd/E19455-01/806-3773/instructionset-130/index.html
                    mRegisters.IP += 1 ' Is this right???
                End If
                HandleHardwareInterrupt(pendingIntNum)
            End If
        End If
    End Sub

    Private Sub HandleInterrupt(intNum As Byte, isHard As Boolean)
        If Not (intHooks.ContainsKey(intNum) AndAlso intHooks(intNum).Invoke()) Then
            PushIntoStack(mFlags.EFlags)
            PushIntoStack(mRegisters.CS)

            If isHard Then
                PushIntoStack(mRegisters.IP - newPrefixLast)
            Else
                PushIntoStack(mRegisters.IP + opCodeSize)
            End If

            tmpUVal1 = intNum * 4
            IPAddrOffet = RAM16(0, tmpUVal1,, True)
            mRegisters.CS = RAM16(0, tmpUVal1, 2, True)

            If intNum = 0 Then ThrowException("Division By Zero")
        End If

        'If intNum = 19 Then DebugMode = True

        mFlags.IF = 0
        mFlags.TF = 0

        clkCyc += 51
    End Sub
End Class