'The MAIN Guide: http://docs.huihoo.com/help-pc/index.html or http://stanislavs.org/helppc/ (mirror)
' http://www.delorie.com/djgpp/doc/rbinter/ix/

Partial Public Class X8086
    Private lastAH(256 - 1) As UInt16
    Private lastCF(256 - 1) As Byte

    Public Sub HandleHardwareInterrupt(intNum As Byte, Optional ignoreHooks As Boolean = False)
        HandleInterrupt(intNum, True, ignoreHooks)
        mRegisters.IP = IPAddrOffset
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

    ' Divide error (INT 0), raised by DIV, IDIV and AAM.
    ' On the 8086/8088 this is taken *after* the instruction, like a software INT: the pushed
    ' return address points to the NEXT instruction (verified against all 15,154 divide-error
    ' cases in the 8088 ProcessorTests). The 80186/286 and later changed this to a fault that
    ' pushes the address of the dividing instruction itself.
    Private Sub RaiseDivideError()
        HandleInterrupt(0, False)
    End Sub

    Public Sub HandleInterrupt(intNum As Byte, isHard As Boolean, Optional ignoreHooks As Boolean = False)
        If Not ((Not ignoreHooks) AndAlso intHooks.ContainsKey(intNum) AndAlso intHooks(intNum).Invoke()) Then
            PushIntoStack(mFlags.EFlags)
            PushIntoStack(mRegisters.CS)
            PushIntoStack(mRegisters.IP + If(isHard, -newPrefixLast, opCodeSize))

            tmpUVal1 = intNum * 4
            IPAddrOffset = RAM16(0, tmpUVal1,, True)
            mRegisters.CS = RAM16(0, tmpUVal1, 2, True)

            If intNum = 0 Then ThrowException("Division By Zero")

            ' Entering the handler disables interrupts and single-stepping. When a hook services the interrupt
            ' instead, the caller continues as if the handler's IRET had already restored its flags.
            mFlags.IF = 0
            mFlags.TF = 0
        End If

        clkCyc += 51
    End Sub
End Class