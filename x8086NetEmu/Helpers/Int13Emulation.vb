Partial Public Class x8086
    Private Sub HandleINT13()
        If mFloppyController Is Nothing Then DiskAdapterNotFound()

        Dim ret As Integer
        Dim AL As Integer

        ' Select floppy drive
        Dim dskImg As DiskImage = mFloppyController.DiskImage(mRegisters.DL)
        Dim bufSize As Integer = mRegisters.AL * If(dskImg IsNot Nothing, dskImg.SectorSize, 0)

        If mRegisters.AH > 8 Then Stop

        Select Case mRegisters.AH
            Case &H0 ' reset drive
                x8086.Notify("Drive {0} Reset", NotificationReasons.Info, mRegisters.DL)
                ret = If(dskImg Is Nothing, &HAA, 0)

            Case &H1 ' get last operation status
                x8086.Notify("Drive {0} Get Last Operation Status", NotificationReasons.Info, mRegisters.DL)
                mRegisters.AH = lastAH(mRegisters.DL)
                mFlags.CF = lastCF(mRegisters.DL)
                ret = 0

            Case &H2  ' read sectors
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                Dim address As Integer = x8086.SegOffToAbs(mRegisters.ES, mRegisters.BX)
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
                AL = bufSize / dskImg.SectorSize

            Case &H3 ' write sectors
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                If dskImg.IsReadOnly Then
                    x8086.Notify("Write Sectors: Drive {0} Failed / Read Only", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H3 ' write protected
                    Exit Select
                End If

                Dim address As Integer = x8086.SegOffToAbs(mRegisters.ES, mRegisters.BX)
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
                AL = bufSize / dskImg.SectorSize

            Case &H4 ' Verify Sectors
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                Dim address As Integer = x8086.SegOffToAbs(mRegisters.ES, mRegisters.BX)
                Dim offset As Long = dskImg.LBA(mRegisters.CH, mRegisters.DH, mRegisters.CL)

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    x8086.Notify("Format Track: Drive {0} Seek Failed", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                x8086.Notify("Drive {0} Verify Sectors H{1:00} T{2:000} S{3:000} ? {4:000} {5:000000} ? {6}:{7}", NotificationReasons.Info,
                                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL,
                                offset.ToHex(DataSize.DWord).Replace("h", ""),
                                mRegisters.ES.ToString("X4"),
                                mRegisters.BX.ToString("X4"))

                AL = bufSize / dskImg.SectorSize
                ret = 0

            Case &H5 ' Format Track
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                Dim address As Integer = x8086.SegOffToAbs(mRegisters.ES, mRegisters.BX)
                Dim offset As Long = dskImg.LBA(mRegisters.CH, mRegisters.DH, mRegisters.CL)

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    x8086.Notify("Format Track: Drive {0} Seek Failed", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                x8086.Notify("Drive {0} Format Track H{1:00} T{2:000} S{3:000} ? {4:000} {5:000000} = {6}:{7}", NotificationReasons.Info,
                                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL,
                                offset.ToHex(DataSize.DWord).Replace("h", ""),
                                mRegisters.ES.ToString("X4"),
                                mRegisters.BX.ToString("X4"))
                ret = 0

            Case &H6 ' Format Track - Set Bad Sector Flag
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                x8086.Notify("Drive {0} Format Track (SBSF) H{1:00} T{2:000} S{3:000} ? {4:000}", NotificationReasons.Info,
                                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL)
                ret = 0

            Case &H7 ' Format Drive Starting at Track
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                x8086.Notify("Drive {0} Format Drive H{1:00} T{2:000} S{3:000}", NotificationReasons.Info,
                                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL)
                ret = 0

            Case &H8 ' Get Drive Parameters
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
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

            Case &H9 ' Init Drive Pair Characteristic
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If
                x8086.Notify("Drive {0} Init Drive Pair Characteristic", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            ' The following are meant to keep diagnostic tools happy ;)

            Case &HA ' Read Long Sectors
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                Dim address As Integer = x8086.SegOffToAbs(mRegisters.ES, mRegisters.BX)
                Dim offset As Long = dskImg.LBA(mRegisters.CH, mRegisters.DH, mRegisters.CL)

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    x8086.Notify("Read Sectors Long: Drive {0} Seek Fail", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                x8086.Notify("Drive {0} Read Long H{1:00} T{2:000} S{3:000} x {4:000} {5:000000} -> {6}:{7}", NotificationReasons.Info,
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
                    x8086.Notify("Read Sectors Long: Drive {0} CRC Error", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H10 ' CRC error
                    Exit Select
                ElseIf ret = DiskImage.EOF Then
                    x8086.Notify("Read Sectors Long: Drive {0} Sector Not Found", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H4 ' sector not found
                    Exit Select
                End If
                Dim ecc As Byte() = BitConverter.GetBytes(buf.Sum(Function(b) b))
                ReDim Preserve buf(buf.Length + 4)
                buf(buf.Length - 4) = ecc(1)
                buf(buf.Length - 3) = ecc(0)
                buf(buf.Length - 2) = ecc(3)
                buf(buf.Length - 1) = ecc(2)
                CopyToRAM(buf, address)
                AL = bufSize / dskImg.SectorSize

            Case &HC ' Seek to Cylinder
                x8086.Notify("Drive {0} Seek to Cylinder ", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &HD ' Alternate Disk Reset
                x8086.Notify("Drive {0} Alternate Disk Reset", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H14 ' Controller Internal Diagnostic
                x8086.Notify("Drive {0} Controller Internal Diagnostic", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H11 ' Recalibrate
                x8086.Notify("Drive {0} Recalibrate", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H15 ' Read DASD Type
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
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

            Case &H12 ' Controller RAM Diagnostic
                x8086.Notify("Drive {0} Controller RAM Diagnostic", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H13 ' Drive Diagnostic
                x8086.Notify("Drive {0} Drive Diagnostic", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H41 ' Check Extensions Support
                x8086.Notify("Drive {0} Extensions Check", NotificationReasons.Info, mRegisters.DL)
                If mRegisters.BX = &H55AA Then
                    mFlags.CF = 0
                    mRegisters.AH = &H1
                    mRegisters.CX = &H4
                Else
                    mFlags.CF = 1
                    mRegisters.AH = &HFF
                End If
                Exit Sub

            Case &H42 ' Extended Sectors Read
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                Dim dap As UInteger = x8086.SegOffToAbs(mRegisters.DS, mRegisters.SI)
                bufSize = RAM(dap + 3) << 8 Or RAM(dap + 2)
                Dim seg As Integer = RAM(dap + 7) << 8 Or RAM(dap + 6)
                Dim off As Integer = RAM(dap + 5) << 8 Or RAM(dap + 4)
                Dim address As UInteger = x8086.SegOffToAbs(seg, off)
                Dim offset As Long = RAM(dap + &HF) << 56 Or RAM(dap + &HE) << 48 Or
                                     RAM(dap + &HD) << 40 Or RAM(dap + &HC) << 32 Or
                                     RAM(dap + &HB) << 24 Or RAM(dap + &HA) << 16 Or
                                     RAM(dap + &H9) << 8 Or RAM(dap + &H8)

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    x8086.Notify("Read Sectors: Drive {0} Seek Fail", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                x8086.Notify("Drive {0} Read {4:000} {5:000000} -> {6}:{7}", NotificationReasons.Info,
                                mRegisters.DL,
                                bufSize,
                                offset.ToHex(DataSize.DWord, ""),
                                seg.ToString("X4"),
                                off.ToString("X4"))

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
                AL = bufSize / dskImg.SectorSize

            Case &H43 ' Extended Sectors Write
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                Dim dap As UInteger = x8086.SegOffToAbs(mRegisters.DS, mRegisters.SI)
                bufSize = RAM(dap + 3) << 8 Or RAM(dap + 2)
                Dim seg As Integer = RAM(dap + 7) << 8 Or RAM(dap + 6)
                Dim off As Integer = RAM(dap + 5) << 8 Or RAM(dap + 4)
                Dim address As UInteger = x8086.SegOffToAbs(seg, off)
                Dim offset As Long = RAM(dap + &HF) << 56 Or RAM(dap + &HE) << 48 Or
                                     RAM(dap + &HD) << 40 Or RAM(dap + &HC) << 32 Or
                                     RAM(dap + &HB) << 24 Or RAM(dap + &HA) << 16 Or
                                     RAM(dap + &H9) << 8 Or RAM(dap + &H8)

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    x8086.Notify("Write Sectors: Drive {0} Seek Fail", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                x8086.Notify("Drive {0} Write {4:000} {5:000000} <- {6}:{7}", NotificationReasons.Info,
                                mRegisters.DL,
                                bufSize,
                                offset.ToHex(DataSize.DWord, ""),
                                seg.ToString("X4"),
                                off.ToString("X4"))

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
                AL = bufSize / dskImg.SectorSize

            Case &H48 ' Extended get Drive Parameters
                If dskImg Is Nothing Then
                    x8086.Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                If dskImg.Tracks <= 0 Then
                    x8086.Notify("Get Drive Parameters: Drive {0} Unknown Geometry", NotificationReasons.Warn, mRegisters.DL)
                    ret = &HAA
                Else
                    Throw New NotImplementedException("Extended get Drive Parameters is not Implemented")
                    x8086.Notify("Drive {0} Get Parameters", NotificationReasons.Info, mRegisters.DL)
                    ret = 0
                End If

            Case Else
                x8086.Notify("Drive {0} Unknown Request {1}", NotificationReasons.Err,
                                                            mRegisters.DL,
                                                            ((mRegisters.AX And &HFF00) >> 8).ToHex(DataSize.Byte))
                ret = &H1
        End Select

        ' Store return status
        If mRegisters.AH <> 0 Then
            RAM8(&H40, &H41) = ret And &HFF
            mRegisters.AX = ret << 8 Or AL
        End If
        mFlags.CF = If(ret <> 0, 1, 0)

        lastAH(mRegisters.DL) = mRegisters.AH
        lastCF(mRegisters.DL) = mFlags.CF

        If (mRegisters.DL And &H80) <> 0 Then Memory(&H474) = mRegisters.AH
    End Sub
End Class
