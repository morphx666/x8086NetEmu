Imports System.IO
Imports System.Runtime.InteropServices

Public Class HostFolderAsDisk
    Inherits DiskImage

    Private Const sectorsPerTrack As UInt16 = 17
    Private Const mediaDescriptor As Byte = &HF8
    Private Const volumeLabel As String = "X8086"

    Private ReadOnly mDiskSize As Integer = 17 * 1024 * 1204

    Private ReadOnly ssdf As StandardDiskFormat

    Public Sub New(folderLocation As String, Optional mountInReadOnlyMode As Boolean = False)
        MyBase.New()
        imgDataStrm = New FileStream(Path.Combine(folderLocation, "vfs.dat"), FileMode.OpenOrCreate, FileAccess.ReadWrite)
        mFileLength = mDiskSize

        CreateBootSector()
        CreatePartitions()

        ssdf = New StandardDiskFormat(imgDataStrm, True)
        'ssdf.SetVolumeLabel(0, volumeLabel)

        MyBase.OpenImage(mountInReadOnlyMode, True)
    End Sub

    Private Sub CreatePartitions()
        Dim bsSize As Integer = Marshal.SizeOf(GetType(StandardDiskFormat.MBR))
        Dim pb As GCHandle
        Dim b(bsSize - 1) As Byte
        Dim bc(446 - 1) As Byte

        Dim mbr As New StandardDiskFormat.MBR With {
            .BootCode = bc,
            .Signature = &HAA55
        }
        ReDim mbr.Partitions(4 - 1)

        For i As Integer = 0 To 4 - 1
            Dim p As New StandardDiskFormat.Partition()
            If i = 0 Then
                p.BootIndicator = StandardDiskFormat.BootIndicators.SystemPartition
                p.EndingHead = 3
                p.EndingSectorCylinder = 65361
                p.RelativeSector = sectorsPerTrack
                p.StartingHead = 1
                p.StartingSectorCylinder = 1
                p.SystemId = StandardDiskFormat.SystemIds.FAT_16
                p.TotalSectors = 34799
            End If
            mbr.Partitions(i) = p
        Next

        pb = GCHandle.Alloc(b, GCHandleType.Pinned)
        Marshal.StructureToPtr(Of StandardDiskFormat.MBR)(mbr, pb.AddrOfPinnedObject(), True)
        pb.Free()

        imgDataStrm.Position = 0
        imgDataStrm.Write(b, 0, b.Length)
    End Sub

    Private Sub CreateBootSector()
        Dim bsSize As Integer = Marshal.SizeOf(GetType(FAT16.BootSector))
        Dim pb As GCHandle
        Dim b(bsSize - 1) As Byte
        Dim bc(448 - 1) As Byte

        Dim bpb As New FAT16.ParameterBlock() With {
            .BytesPerSector = 512,
            .HeadsPerCylinder = 4,
            .HiddenSectors = 17,
            .MaxRootEntries = 512,
            .MediaDescriptor = mediaDescriptor,
            .NumberOfFATCopies = 2,
            .ReservedSectors = 1,
            .SectorsPerCluster = 4,
            .SectorsPerFAT = 34,
            .SectorsPerTrack = sectorsPerTrack,
            .TotalSectors = 34799
        }

        Dim ebpb As New FAT16.ExtendedParameterBlock() With {
            .DriveNumber = 0,
            .Reserved = &H80,
            .ExtendedBootSignature = 41,
            .SerialNumber = 1234567,
            .VolumeLabel = volumeLabel,
            .FileSystemType = "FAT16"
        }

        Dim bs As New FAT16.BootSector With {
            .Signature = &HAA55,
            .BootStrapCode = bc,
            .JumpCode = {&HEB, &H3C, &H90},
            .ExtendedBIOSParameterBlock = ebpb,
            .BIOSParameterBlock = bpb,
            .OemId = "XFXJS"
        }

        pb = GCHandle.Alloc(b, GCHandleType.Pinned)
        Marshal.StructureToPtr(Of FAT16.BootSector)(bs, pb.AddrOfPinnedObject(), True)
        pb.Free()

        imgDataStrm.Position = sectorsPerTrack * 512
        imgDataStrm.Write(b, 0, b.Length)
    End Sub
End Class