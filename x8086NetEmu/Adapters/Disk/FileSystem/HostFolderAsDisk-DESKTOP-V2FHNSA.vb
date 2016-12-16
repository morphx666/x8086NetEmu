﻿Imports System.Runtime.InteropServices

Public Class HostFolderAsDisk
    Inherits DiskImage

    Private mDiskSize As UInteger = 32 * 1024 * 1204 ' 32MB
    Private bytesPerSector As UShort = 512
    Private buffer() As Byte

    Private s As IO.FileStream
    Private image As StandardDiskFormat.MasterBootRecord

    Private Structure test
        Public Shared test(8) As Byte
    End Structure

    Public Sub New(fileName As String, Optional mountInReadOnlyMode As Boolean = False)
        mFileName = fileName

        s = New IO.FileStream(mFileName, IO.FileMode.Open)
        image = New StandardDiskFormat.MasterBootRecord(s, 0)
        s.Seek(0, IO.SeekOrigin.Begin)

        mSectorSize = CType(image.Partitions(0).FileSystem, FAT12_16.BootSector).BIOSParameterBlock.BytesPerSector
        mCylinders = image.Partitions(0).Cylinders + 1
        mHeads = image.Partitions(0).Heads * 2
        mSectors = image.Partitions(0).Sectors + 1
        mFileLength = s.Length

        mReadOnly = mountInReadOnlyMode
        mIsHardDisk = CType(image.Partitions(0).FileSystem, FAT12_16.BootSector).BIOSParameterBlock.MediaDescriptor = &HF8
        mStatus = ImageStatus.DiskLoaded

        x8086.Notify("DiskImage '{0}': {1}", x8086.NotificationReasons.Info, mFileName, mStatus.ToString())
    End Sub

    Public Overrides Function Read(offset As Long, data() As Byte) As Integer
        If mStatus <> ImageStatus.DiskLoaded Then Return -1

        If offset < 0 OrElse offset + data.Length > mFileLength Then Return EOF

        Try
            s.Seek(offset, IO.SeekOrigin.Begin)
            s.Read(data, 0, data.Length)

            Return 0
        Catch e As Exception
            Return EIO
        End Try
    End Function

    Public Overrides Function Write(offset As Long, data() As Byte) As Integer
        'If mStatus <> ImageStatus.DiskLoaded Then Return -1

        'If offset < 0 OrElse offset + data.Length > mFileLength Then Return EOF

        'Try
        '    file.Seek(offset, IO.SeekOrigin.Begin)
        '    file.Write(data, 0, data.Length)
        '    Return 0
        'Catch e As Exception
        '    Return EIO
        'End Try
    End Function
End Class
