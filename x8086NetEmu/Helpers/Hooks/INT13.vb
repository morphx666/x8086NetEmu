﻿' http://www.delorie.com/djgpp/doc/rbinter/ix/13/

Partial Public Class X8086
    Private Function HandleINT13() As Boolean
        If mFloppyController Is Nothing Then
            ThrowException("Disk Adapter Not Found")
            Return True
        End If

        Dim ret As Integer
        Dim AL As Integer
        Dim offset As Long

        Dim dskImg As DiskImage = mFloppyController.DiskImage(mRegisters.DL)
        Dim bufSize As Integer

        Select Case mRegisters.AH
            Case &H0 ' Reset drive
                Notify("Drive {0:000} Reset", NotificationReasons.Info, mRegisters.DL)
                ret = If(dskImg Is Nothing, &HAA, 0)

            Case &H1 ' Get last operation status
                Notify("Drive {0:000} Get Last Operation Status", NotificationReasons.Info, mRegisters.DL)
                mRegisters.AH = lastAH(mRegisters.DL)
                mFlags.CF = lastCF(mRegisters.DL)
                ret = 0

            Case &H2 ' Read sectors
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                offset = dskImg.LBA(mRegisters.CH, mRegisters.DH, mRegisters.CL)
                bufSize = mRegisters.AL * dskImg.SectorSize

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    Notify("Read Sectors: Drive {0:000} Seek Fail", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                Notify("Drive {0:000} Read  H{1:00} T{2:000} S{3:000} x {4:000} {5:X6} -> {6:X4}:{7:X4}", NotificationReasons.Info,
                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL,
                                offset,
                                mRegisters.ES,
                                mRegisters.BX)

                Dim buf(bufSize - 1) As Byte
                ret = dskImg.Read(offset, buf)
                If ret = DiskImage.EIO Then
                    Notify("Read Sectors: Drive {0:000} CRC Error", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H10 ' CRC error
                    Exit Select
                ElseIf ret = DiskImage.EOF Then
                    Notify("Read Sectors: Drive {0:000} Sector Not Found", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H4 ' sector not found
                    Exit Select
                End If
                CopyToMemory(buf, SegmentOffsetToAbsolute(mRegisters.ES, mRegisters.BX))
                AL = bufSize \ dskImg.SectorSize

            Case &H3 ' Write sectors
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                If dskImg.IsReadOnly Then
                    Notify("Write Sectors: Drive {0:000} Failed / Read Only", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H3 ' write protected
                    Exit Select
                End If

                offset = dskImg.LBA(mRegisters.CH, mRegisters.DH, mRegisters.CL)
                bufSize = mRegisters.AL * dskImg.SectorSize

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    Notify("Write Sectors: Drive {0:000} Seek Failed", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                Notify("Drive {0:000} Write H{1:00} T{2:000} S{3:000} x {4:000} {5:X6} <- {6:X4}:{7:X4}", NotificationReasons.Info,
                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL,
                                offset,
                                mRegisters.ES,
                                mRegisters.BX)

                Dim buf(bufSize - 1) As Byte
                CopyFromMemory(buf, SegmentOffsetToAbsolute(mRegisters.ES, mRegisters.BX))
                ret = dskImg.Write(offset, buf)
                If ret = DiskImage.EIO Then
                    Notify("Write Sectors: Drive {0:000} CRC Error", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H10 ' CRC error
                    Exit Select
                ElseIf ret = DiskImage.EOF Then
                    Notify("Write Sectors: Drive {0:000} Sector Not Found", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H4 ' sector not found
                    Exit Select
                End If
                AL = bufSize \ dskImg.SectorSize

            Case &H4 ' Verify Sectors
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                offset = dskImg.LBA(mRegisters.CH, mRegisters.DH, mRegisters.CL)
                bufSize = mRegisters.AL * dskImg.SectorSize

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    Notify("Verify Sector: Drive {0} Seek Failed", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                Notify("Drive {0:000} Verify Sectors H{1:00} T{2:000} S{3:000} ? {4:000} {5:X6} ? {6:X4}:{7:X4}", NotificationReasons.Info,
                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL,
                                offset,
                                mRegisters.ES,
                                mRegisters.BX)

                AL = bufSize \ dskImg.SectorSize
                ret = 0

            Case &H5 ' Format Track
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                offset = dskImg.LBA(mRegisters.CH, mRegisters.DH, mRegisters.CL)
                bufSize = mRegisters.AL * dskImg.SectorSize

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    Notify("Format Track: Drive {0:000} Seek Failed", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                Notify("Drive {0:000} Format Track H{1:00} T{2:000} S{3:000} ? {4:000} {5:X6} = {6:X4}:{7:X4}", NotificationReasons.Info,
                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL,
                                offset,
                                mRegisters.ES,
                                mRegisters.BX)
                ret = 0

            Case &H6 ' Format Track - Set Bad Sector Flag
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                Notify("Drive {0:000} Format Track (SBSF) H{1:00} T{2:000} S{3:000} ? {4:000}", NotificationReasons.Info,
                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL)
                ret = 0

            Case &H7 ' Format Drive Starting at Track
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                Notify("Drive {0:000} Format Drive H{1:00} T{2:000} S{3:000}", NotificationReasons.Info,
                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL)
                ret = 0

            Case &H8 ' Get Drive Parameters
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                If dskImg.Tracks <= 0 Then
                    Notify("Get Drive Parameters: Drive {0:000} Unknown Geometry", NotificationReasons.Warn, mRegisters.DL)
                    ret = &HAA
                Else
                    mRegisters.CH = (dskImg.Cylinders - 1) And &HFF
                    mRegisters.CL = dskImg.Sectors And 63
                    mRegisters.CL += ((dskImg.Cylinders - 1) \ 256) * 64
                    mRegisters.DH = dskImg.Heads - 1

                    If mRegisters.DL < &H80 Then
                        mRegisters.BL = 4
                        mRegisters.DL = 2
                    Else
                        mRegisters.DL = DiskImage.HardDiskCount
                    End If

                    Notify("Drive {0:000} Get Parameters", NotificationReasons.Info, mRegisters.DL)
                    ret = 0
                End If

            Case &H9 ' Initialize Drive Pair Characteristic
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If
                Notify("Drive {0:000} Init Drive Pair Characteristic", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            ' The following are meant to keep diagnostic tools happy ;)

            Case &HA ' Read Long Sectors
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                offset = dskImg.LBA(mRegisters.CH, mRegisters.DH, mRegisters.CL)
                bufSize = mRegisters.AL * dskImg.SectorSize

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    Notify("Read Sectors Long: Drive {0:000} Seek Fail", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                Notify("Drive {0:000} Read Long H{1:00} T{2:000} S{3:000} x {4:000} {5:X6} -> {6:X4}:{7:X4}", NotificationReasons.Info,
                mRegisters.DL,
                                mRegisters.DH,
                                mRegisters.CH,
                                mRegisters.CL,
                                mRegisters.AL,
                                offset,
                                mRegisters.ES,
                                mRegisters.BX)

                Dim buf(bufSize - 1) As Byte
                ret = dskImg.Read(offset, buf)
                If ret = DiskImage.EIO Then
                    Notify("Read Sectors Long: Drive {0:000} CRC Error", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H10 ' CRC error
                    Exit Select
                ElseIf ret = DiskImage.EOF Then
                    Notify("Read Sectors Long: Drive {0:000} Sector Not Found", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H4 ' sector not found
                    Exit Select
                End If
                Dim ecc As Byte() = BitConverter.GetBytes(buf.Sum(Function(b) b))
                ReDim Preserve buf(buf.Length + 4)
                buf(buf.Length - 4) = ecc(1)
                buf(buf.Length - 3) = ecc(0)
                buf(buf.Length - 2) = ecc(3)
                buf(buf.Length - 1) = ecc(2)
                CopyToMemory(buf, SegmentOffsetToAbsolute(mRegisters.ES, mRegisters.BX))
                AL = bufSize \ dskImg.SectorSize

            Case &HC ' Seek to Cylinder
                Notify("Drive {0:000} Seek to Cylinder ", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &HD ' Alternate Disk Reset
                Notify("Drive {0:000} Alternate Disk Reset", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H14 ' Controller Internal Diagnostic
                Notify("Drive {0:000} Controller Internal Diagnostic", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H11 ' Recalibrate
                Notify("Drive {0:000} Recalibrate", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H15 ' Read DASD Type
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                If mRegisters.DL < &H80 Then
                    ret = &H64
                Else
                    mRegisters.CX = dskImg.Sectors \ 256
                    mRegisters.DX = dskImg.Sectors And &HFF
                    ret = &H12C
                End If
                Notify("Drive {0:000} Read DASD Type", NotificationReasons.Info, mRegisters.DL)

            Case &H12 ' Controller RAM Diagnostic
                Notify("Drive {0:000} Controller RAM Diagnostic", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H13 ' Drive Diagnostic
                Notify("Drive {0:000} Drive Diagnostic", NotificationReasons.Info, mRegisters.DL)
                ret = 0

            Case &H41 ' Check Extensions Support
                Notify("Drive {0:000} Extensions Check", NotificationReasons.Info, mRegisters.DL)
                If mRegisters.BX = &H55AA Then
                    mFlags.CF = 0
                    mRegisters.AH = &H1
                    mRegisters.CX = &H4
                Else
                    mFlags.CF = 1
                    mRegisters.AH = &HFF
                End If
                Return True

            Case &H42 ' Extended Sectors Read
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                Dim dap As UInt32 = SegmentOffsetToAbsolute(mRegisters.DS, mRegisters.SI)
                bufSize = RAM(dap + 3) << 8 Or RAM(dap + 2)
                Dim seg As Integer = RAM(dap + 7) << 8 Or RAM(dap + 6)
                Dim off As Integer = RAM(dap + 5) << 8 Or RAM(dap + 4)
                offset = RAM(dap + &HF) << 56 Or RAM(dap + &HE) << 48 Or
                                     RAM(dap + &HD) << 40 Or RAM(dap + &HC) << 32 Or
                                     RAM(dap + &HB) << 24 Or RAM(dap + &HA) << 16 Or
                                     RAM(dap + &H9) << 8 Or RAM(dap + &H8)

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    Notify("Read Sectors: Drive {0:000} Seek Fail", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                Notify("Drive {0:000} Read {4:000} {5:X6} -> {6:X4}:{7:X4}", NotificationReasons.Info,
                mRegisters.DL,
                                bufSize,
                                offset,
                                seg,
                                off)

                Dim buf(bufSize - 1) As Byte
                ret = dskImg.Read(offset, buf)
                If ret = DiskImage.EIO Then
                    Notify("Read Sectors: Drive {0:000} CRC Error", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H10 ' CRC error
                    Exit Select
                ElseIf ret = DiskImage.EOF Then
                    Notify("Read Sectors: Drive {0:000} Sector Not Found", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H4 ' sector not found
                    Exit Select
                End If
                CopyToMemory(buf, SegmentOffsetToAbsolute(seg, off))
                AL = bufSize \ dskImg.SectorSize

            Case &H43 ' Extended Sectors Write
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                Dim dap As UInt32 = SegmentOffsetToAbsolute(mRegisters.DS, mRegisters.SI)
                bufSize = RAM(dap + 3) << 8 Or RAM(dap + 2)
                Dim seg As Integer = RAM(dap + 7) << 8 Or RAM(dap + 6)
                Dim off As Integer = RAM(dap + 5) << 8 Or RAM(dap + 4)
                offset = RAM(dap + &HF) << 56 Or RAM(dap + &HE) << 48 Or
                                     RAM(dap + &HD) << 40 Or RAM(dap + &HC) << 32 Or
                                     RAM(dap + &HB) << 24 Or RAM(dap + &HA) << 16 Or
                                     RAM(dap + &H9) << 8 Or RAM(dap + &H8)

                If offset < 0 OrElse offset + bufSize > dskImg.FileLength Then
                    Notify("Write Sectors: Drive {0:000} Seek Fail", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H40 ' seek failed
                    Exit Select
                End If

                Notify("Drive {0:000} Write {4:000} {5:X6} <- {6:X4}:{7:X4}", NotificationReasons.Info,
                mRegisters.DL,
                                bufSize,
                                offset,
                                seg,
                                off)

                Dim buf(bufSize - 1) As Byte
                CopyFromMemory(buf, SegmentOffsetToAbsolute(seg, off))
                ret = dskImg.Write(offset, buf)
                If ret = DiskImage.EIO Then
                    Notify("Write Sectors: Drive {0:000} CRC Error", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H10 ' CRC error
                    Exit Select
                ElseIf ret = DiskImage.EOF Then
                    Notify("Write Sectors: Drive {0:000} Sector Not Found", NotificationReasons.Warn, mRegisters.DL)
                    ret = &H4 ' sector not found
                    Exit Select
                End If
                AL = bufSize \ dskImg.SectorSize

            Case &H48 ' Extended get Drive Parameters
                If dskImg Is Nothing Then
                    Notify("Invalid Drive Number: Drive {0:000} Not Ready", NotificationReasons.Info, mRegisters.DL)
                    ret = &HAA ' fixed disk drive not ready
                    Exit Select
                End If

                If dskImg.Tracks <= 0 Then
                    Notify("Get Drive Parameters: Drive {0:000} Unknown Geometry", NotificationReasons.Warn, mRegisters.DL)
                    ret = &HAA
                Else
                    Throw New NotImplementedException("Extended get Drive Parameters is not Implemented")
                    Notify("Drive {0:000} Get Parameters", NotificationReasons.Info, mRegisters.DL)
                    ret = 0
                End If

            Case Else
                Notify("Drive {0:000} Unknown Request {1}", NotificationReasons.Err,
                mRegisters.DL,
                                                            ((mRegisters.AX And &HFF00) >> 8).ToString("X2"))
                ret = &H1
        End Select

        If mRegisters.AH <> 0 Then
            RAM8(&H40, &H41) = ret
            mRegisters.AX = (ret << 8) Or AL
        End If
        mFlags.CF = If(ret = 0, 0, 1)

        lastAH(mRegisters.DL) = mRegisters.AH
        lastCF(mRegisters.DL) = mFlags.CF

        If (mRegisters.DL And &H80) <> 0 Then Memory(&H474) = mRegisters.AH

        Return True
    End Function
End Class