'The MAIN Guide: http://docs.huihoo.com/help-pc/index.html
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
           repeLoopMode = REPLoopModes.None AndAlso
           picIsAvailable Then

            Dim pendingIntNum As Byte = PIC.GetPendingInterrupt()
            If pendingIntNum <> &HFF Then
                mIsHalted = False
                HandleHardwareInterrupt(pendingIntNum)
            End If
        End If
    End Sub

    Private Sub HandleInterrupt(intNum As Byte, isHard As Boolean)
        If Not (intHooks.ContainsKey(intNum) AndAlso intHooks(intNum).Invoke()) Then
            PushIntoStack(mFlags.EFlags)
            PushIntoStack(mRegisters.CS)

            If isHard Then
                PushIntoStack(AdjustIP(mRegisters.IP))
            Else
                PushIntoStack(mRegisters.IP + opCodeSize)
            End If

            tmpVal = intNum * 4
            IPAddrOffet = RAM16(0, tmpVal,, True)
            mRegisters.CS = RAM16(0, tmpVal, 2, True)

            If intNum = 0 Then DivisionByZero()
        End If

        mFlags.IF = 0
        mFlags.TF = 0

        clkCyc += 51
    End Sub

    Private Function IsPrefix(opCode As Byte) As Boolean
        Select Case opCode
            Case &H26, &H2E, &H36, &H3E, &HF2, &HF3 : Return True
        End Select
        Return False
    End Function

    Private Function AdjustIP(v As UShort) As UShort
        While IsPrefix(RAM8(mRegisters.CS, v - 1))
            v -= 1
        End While
        Return v
    End Function
End Class