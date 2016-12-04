Imports System.Threading

'The MAIN Guide: http://docs.huihoo.com/help-pc/index.html
' http://www.delorie.com/djgpp/doc/rbinter/ix/

Partial Public Class x8086
    Private ignoreTimeSync As Boolean

    Private lastAH(256 - 1) As UShort
    Private lastCF(256 - 1) As UShort

    Public Sub HandleHardwareInterrupt(intNum As Byte)
        HandleInterrupt(intNum, True)
        mRegisters.IP = IPAddrOff
    End Sub

    Private Sub HandlerPendingInterrupt()
        ' Lesson 5 (mRegisters.ActiveSegmentChanged = False)
        ' http://ntsecurity.nu/onmymind/2007/2007-08-22.html

        If mFlags.IF = 1 AndAlso
           Not ignoreINTs AndAlso
           Not trapEnabled AndAlso
           mRegisters.ActiveSegmentChanged = False AndAlso
           picIsAvailable Then

            Dim intNum As Integer = PIC.GetPendingInterrupt()
            If intNum >= 0 Then
                mIsHalted = False
                HandleHardwareInterrupt(intNum)
            End If
        End If
    End Sub

    Private Sub HandleInterrupt(intNum As Byte, isHard As Boolean)
        FlushCycles()

        If intNum = &H13 AndAlso mEmulateINT13 Then ' (intNum = &H13 OrElse intNum = &HFD) AndAlso mEmulateINT13 Then
            HandleINT13()
        Else
            'If intNum = 1 Then
            '    DebugMode = True ' This is what causes CheckIT to hang when testing the "CPU Interrupt Bug"
            '    'Stop
            'End If

            PushIntoStack(mFlags.EFlags)
            PushIntoStack(mRegisters.CS)

            If isHard Then
                PushIntoStack(mRegisters.IP)
            Else
                PushIntoStack(AddValues(mRegisters.IP, opCodeSize, DataSize.Word))
            End If

            Dim intOffset As UShort = intNum * 4
            IPAddrOff = RAM16(0, intOffset)
            mRegisters.CS = RAM16(0, AddValues(intOffset, 2, DataSize.Word))

            If intNum = 0 Then ThrowException("Division by Zero")
        End If

        mFlags.IF = 0
        mFlags.TF = 0

        clkCyc += 51
    End Sub

    Private Sub HandleINT13()
        If mFloppyController Is Nothing Then DiskAdapterNotFound()

        Dim ret As Integer

        ' Select floppy drive
        Dim dskImg As DiskImage = mFloppyController.DiskImage(mRegisters.DL)
        Dim bufSize As Integer = mRegisters.AL * If(dskImg IsNot Nothing, dskImg.SectorSize, 0)

        Select Case mRegisters.AH
            Case &H0 ' reset drive
                x8086.Notify("Drive {0} Reset", NotificationReasons.Info, mRegisters.DL)

                ret = 0

            Case &H1 ' get last operation status
                x8086.Notify("Drive {0} Get Last Operation Status", NotificationReasons.Info, mRegisters.DL)

                mRegisters.AH = lastAH(mRegisters.DL)
                mFlags.CF = lastCF(mRegisters.DL)
                Exit Sub

            Case &H2  ' read sectors
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    'ret = &H80 ' no such drive
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                Dim address As UInteger = x8086.SegOffToAbs(mRegisters.ES, mRegisters.BX)
                Dim offset As Long = dskImg.LBA(mRegisters.CH, mRegisters.DH, mRegisters.CL)

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    x8086.Notify("Read Sectors: Drive {0} Seek Fail", NotificationReasons.Warn, mRegisters.DL)

                    ret = &H40 ' seek failed
                    Exit Select
                End If

                x8086.Notify("Drive {0} Read  H{1:00} T{2:000} S{3:000} x {4:000} {5:000000} -> {6}:{7}", NotificationReasons.Info,
                                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL,
                                offset.ToHex(DataSize.DWord, ""),
                                mRegisters.ES.ToString("X4"),
                                mRegisters.BX.ToString("X4"))

                Dim buf(bufSize - 1) As Byte
                ret = dskImg.Read(offset, buf)

                If ret = DiskImage.EIO Then
                    x8086.Notify("Read Sectors: Drive {0} CRC Error", NotificationReasons.Warn, mRegisters.DL)

                    ret = &H10 ' CRC error
                    Exit Select
                ElseIf ret = DiskImage.EOF Then
                    x8086.Notify("Read Sectors: Drive {0} Sector Not Found", NotificationReasons.Warn, mRegisters.DL)

                    ret = &H4 ' sector not found
                    Exit Select
                End If
                CopyToRAM(buf, address)
                'ret = mRegisters.AL

            Case &H3 ' write sectors
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    'ret = &H80 ' no such drive
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                If dskImg.IsReadOnly Then
                    x8086.Notify("Write Sectors: Drive {0} Failed / Read Only", NotificationReasons.Warn, mRegisters.DL)

                    ret = &H3 ' write protected
                    Exit Select
                End If

                Dim address As UInteger = x8086.SegOffToAbs(mRegisters.ES, mRegisters.BX)
                Dim offset As Long = dskImg.LBA(mRegisters.CH, mRegisters.DH, mRegisters.CL)

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    x8086.Notify("Write Sectors: Drive {0} Seek Failed", NotificationReasons.Warn, mRegisters.DL)

                    ret = &H40 ' seek failed
                    Exit Select
                End If

                x8086.Notify("Drive {0} Write H{1:00} T{2:000} S{3:000} x {4:000} {5:000000} <- {6}:{7}", NotificationReasons.Info,
                                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL,
                                offset.ToHex(DataSize.DWord).Replace("h", ""),
                                mRegisters.ES.ToString("X4"),
                                mRegisters.BX.ToString("X4"))

                Dim buf(bufSize - 1) As Byte
                CopyFromRAM(buf, address)
                ret = dskImg.Write(offset, buf)
                If ret = DiskImage.EIO Then
                    x8086.Notify("Write Sectors: Drive {0} CRC Error", NotificationReasons.Warn, mRegisters.DL)

                    ret = &H10 ' CRC error
                    Exit Select
                ElseIf ret = DiskImage.EOF Then
                    x8086.Notify("Write Sectors: Drive {0} Sector Not Found", NotificationReasons.Warn, mRegisters.DL)

                    ret = &H4 ' sector not found
                    Exit Select
                End If
                'ret = mRegisters.AL

            Case &H4, &H5 ' Format Track
                ret = 0

            Case &H8 ' get drive parameters
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    'ret = &H80 ' no such drive
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                If dskImg.Tracks <= 0 Then
                    x8086.Notify("Get Drive Parameters: Drive {0} Unknown Geometry", NotificationReasons.Warn, mRegisters.DL)

                    ret = &HAA
                Else
                    mRegisters.CH = (dskImg.Cylinders - 1) And &HFF
                    mRegisters.CL = (dskImg.Sectors And 63)
                    mRegisters.CL += (dskImg.Cylinders \ 256) * 64
                    mRegisters.DH = dskImg.Heads - 1

                    If mRegisters.DL < &H80 Then
                        mRegisters.BL = 4
                        mRegisters.DL = 2
                    Else
                        mRegisters.DL = DiskImage.HardDiskCount
                    End If

                    x8086.Notify("Drive {0} Get Parameters", NotificationReasons.Info, mRegisters.DL)

                    ret = 0
                End If

            Case &H15 ' read dasd type
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    'ret = &H80 ' no such drive
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                If mRegisters.DL < &H80 Then
                    If dskImg IsNot Nothing Then
                        ret = &H64
                    Else
                        ret = &HFF
                    End If
                Else
                    Dim n As Integer = dskImg.Sectors
                    mRegisters.CX = n \ 256
                    mRegisters.DX = n And &HFF
                    ret = &H12C
                End If
                x8086.Notify("Drive {0} Read DASD Type", NotificationReasons.Info, mRegisters.DL)

            ' The following are meant to keep diagnostic tools happy ;)

            Case &H11 ' recalibrate
                x8086.Notify("Drive {0} Recalibrate", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &HC ' seek to cylender
                x8086.Notify("Drive {0} Seek to Cylender ", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &HD ' alternate disk reset
                x8086.Notify("Drive {0} Alternate Disk Reset", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H14 ' controller internal diagnostic
                x8086.Notify("Drive {0} Controller Internal Diagnostic", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H12 ' controller ram diagnostic
                x8086.Notify("Drive {0} Controller RAM Diagnostic", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H13 ' drive diagnostic
                x8086.Notify("Drive {0} Drive Diagnostic", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case Else
                x8086.Notify("Drive {0} Unknown Request {1}", NotificationReasons.Err,
                                                            mRegisters.DL,
                                                            ((mRegisters.AX And &HFF00) >> 8).ToHex(DataSize.Byte))

                ret = &H1
        End Select

        ' Store return status
        RAM8(&H40, &H41) = ret And &HFF
        mRegisters.AX = ret << 8
        mFlags.CF = If(ret <> 0, 1, 0)

        lastAH(mRegisters.DL) = mRegisters.AH
        lastCF(mRegisters.DL) = mFlags.CF

        If (mRegisters.DL And &H80) <> 0 Then Memory(&H474) = mRegisters.AH
    End Sub
End Class
