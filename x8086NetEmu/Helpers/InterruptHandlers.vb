﻿'The MAIN Guide: http://docs.huihoo.com/help-pc/index.html or http://stanislavs.org/helppc/ (mirror)
' http://www.delorie.com/djgpp/doc/rbinter/ix/

Partial Public Class X8086
    Private lastAH(256 - 1) As UInt16
    Private lastCF(256 - 1) As Byte

    Public Sub HandleHardwareInterrupt(intNum As Byte, Optional ignoreHooks As Boolean = False)
        HandleInterrupt(intNum, True, ignoreHooks)
        mRegisters.IP = IPAddrOffet
    End Sub

    Private Sub HandlePendingInterrupt(Optional ignoreHooks As Boolean = False)
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
                    'mRegisters.IP += 1 ' Is this right???
                End If
                HandleHardwareInterrupt(pendingIntNum, ignoreHooks)
            End If
        End If
    End Sub

    Public Sub HandleInterrupt(intNum As Byte, isHard As Boolean, Optional ignoreHooks As Boolean = False)
        If Not ((Not ignoreHooks) AndAlso intHooks.ContainsKey(intNum) AndAlso intHooks(intNum).Invoke()) Then
            PushIntoStack(mFlags.EFlags)
            PushIntoStack(mRegisters.CS)
            PushIntoStack(mRegisters.IP + If(isHard, -newPrefixLast, opCodeSize))

            tmpUVal1 = intNum * 4
            IPAddrOffet = RAM16(0, tmpUVal1,, True)
            mRegisters.CS = RAM16(0, tmpUVal1, 2, True)

            If intNum = 0 Then ThrowException("Division By Zero")
        End If

        mFlags.IF = 0
        mFlags.TF = 0

        clkCyc += 51
    End Sub
End Class