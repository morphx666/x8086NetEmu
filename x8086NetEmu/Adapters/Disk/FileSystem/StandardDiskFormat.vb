Imports System.Runtime.InteropServices

' http://www.maverick-os.dk/Mainmenu_NoFrames.html

Public Class StandardDiskFormat
    Public Enum BootIndicators As Byte
        NonBootable = 0
        SystemPartition = &H80
    End Enum

    Public Enum SystemIds As Byte
        EMPTY = 0
        FAT_12 = 1
        XENIX_ROOT = 2
        XENIX_USER = 3
        FAT_16 = 4
        EXTENDED = 5
        FAT_BIGDOS = 6
        NTFS_HPFS = 7
        AIX = 8
        AIX_INIT = 9
        OS2_BOOT_MGR = 10
        PRI_FAT32_INT13 = 11
        EXT_FAT32_INT13 = 12
        EXT_FAT16_INT13 = 14
        PRI_FAT16_INT13 = 15
        OPUS = 16
        CPQ_DIAGNOSTIC = 18
        OMEGA_FS = 20
        SWAP_PARTITION = 21
        NEC_MSDOS = 36
        VENIX = 64
        SFS = 66
        DISK_MANAGER = 80
        NOVEL1 = 81
        CPM_MICROPORT = 82
        GOLDEN_BOW = 86
        SPEEDSTOR = 97
        UNIX_SYSV386 = 99 ' GNU_HURD
        NOVEL2 = 100
        PC_IX = 117
        MINUX_OLD = 128
        MINUX_LINUX = 129
        LINUX_SWAP = 130
        LINUX_NATIVE = 131
        AMOEBA = 147
        AMOEBA_BBT = 148
        BSD_386 = 165
        BSDI_FS = 183
        BSDI_SWAP = 184
        SYRINX = 199
        CP_M = 219
        ACCESS_DOS = 225
        DOS_R_O = 227
        DOS_SECONDARY = 242
        LAN_STEP = 254
        BBT = 255
    End Enum

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Public Structure Partition
        Public BootIndicator As BootIndicators
        Public StartingHead As Byte               ' FEDCBA9876 543210
        Public StartingSectorCylinder As UInt16   ' cccccccccc ssssss
        Public SystemId As SystemIds
        Public EndingHead As Byte
        Public EndingSectorCylinder As UInt16     ' cccccccccc ssssss
        Public RelativeSector As UInt32
        Public TotalSectors As UInt32

        Public Shadows Function ToString() As String
            Return $"{SystemId}: {BootIndicator}"
        End Function
    End Structure

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Public Structure MBR
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=446)> Public BootCode() As Byte
        <MarshalAs(UnmanagedType.ByValArray, ArraySubType:=UnmanagedType.Struct, SizeConst:=4)> Public Partitions() As Partition
        Public Signature As UInt16 ' AA55 = bootable
    End Structure

    Private mMasterBootRecord As MBR
    Private mBootSectors(4 - 1) As FAT12_16.BootSector ' In case we wanted to support additional file systems we should use a inheritable class instead of hard coding it to FAT12/16
    Private mFATDataPointers(4 - 1)() As UInt16
    Private mFATIsClean(4 - 1) As Boolean
    Private mFATReadWriteError(4 - 1) As Boolean
    Private mRootDirectoryEntries(4 - 1)() As FAT12_16.DirectoryEntry

    Private strm As IO.Stream
    Private fatRegionStart(4 - 1) As Long

    Public Sub New(s As IO.Stream)
        Dim pb As GCHandle
        Dim b(512 - 1) As Byte

        strm = s

        strm.Read(b, 0, b.Length)
        pb = GCHandle.Alloc(b, GCHandleType.Pinned)
        mMasterBootRecord = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(MBR))
        pb.Free()

        For partitionNumber As Integer = 0 To 4 - 1
            If mMasterBootRecord.Partitions(partitionNumber).SystemId = StandardDiskFormat.SystemIds.FAT_12 OrElse mMasterBootRecord.Partitions(partitionNumber).SystemId = StandardDiskFormat.SystemIds.FAT_16 Then
                strm.Position = mMasterBootRecord.Partitions(partitionNumber).RelativeSector * 512
                strm.Read(b, 0, b.Length)
                pb = GCHandle.Alloc(b, GCHandleType.Pinned)
                mBootSectors(partitionNumber) = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(FAT12_16.BootSector))
                pb.Free()

                fatRegionStart(partitionNumber) = strm.Position

                ReDim mFATDataPointers(partitionNumber)(mBootSectors(partitionNumber).BIOSParameterBlock.SectorsPerFAT * mBootSectors(partitionNumber).BIOSParameterBlock.BytesPerSector / 2 - 1)
                For j As Integer = 0 To mFATDataPointers(partitionNumber).Length - 1
                    mFATDataPointers(partitionNumber)(j) = BitConverter.ToUInt16({strm.ReadByte(), strm.ReadByte()}, 0)
                Next

                If (mFATDataPointers(partitionNumber)(0) And &HFF) = mBootSectors(partitionNumber).BIOSParameterBlock.MediaDescriptor Then
                    mFATIsClean(partitionNumber) = (mFATDataPointers(partitionNumber)(1) And &H8000) <> 0
                    mFATReadWriteError(partitionNumber) = (mFATDataPointers(partitionNumber)(1) And &H4000) = 0

                    'strm.Position += (mBootSectors(partitionNumber).BIOSParameterBlock.NumberOfFATCopies - 1) * mFATDataPointers(partitionNumber).Length * 2
                    mRootDirectoryEntries(partitionNumber) = GetDirectoryEntries(partitionNumber, -6) ' -6 = Root Directory (formula abobe)
                Else
                    ' Invalid boot sector
                End If
            End If
        Next

        'Dim file() As Byte
        'For Each de In mRootDirectoryEntries(0)
        '    If de.FileName = "TEST" AndAlso de.FileExtension = "ASM" Then
        '        file = ReadFile(0, de)
        '        Dim contents As String = Text.Encoding.ASCII.GetString(file)
        '        Stop
        '        Exit For
        '    ElseIf (de.Attribute And FAT12_16.EntryAttributes.Directory) = FAT12_16.EntryAttributes.Directory Then
        '        If de.FileName = "DOS" Then
        '            ' EC00
        '            Dim des() As FAT12_16.DirectoryEntry = GetDirectoryEntries(0, de.StartingCluster)
        '            Stop
        '        End If
        '    End If
        'Next
    End Sub

    Private Function GetDirectoryEntries(partitionNumber As Integer, clusterIndex As Integer) As FAT12_16.DirectoryEntry()
        Dim pb As GCHandle
        Dim des() As FAT12_16.DirectoryEntry = Nothing
        Dim b(32 - 1) As Byte

        strm.Position = ClusterToSector(partitionNumber, clusterIndex)

        Dim dirEntryCount As Integer = 0
        Do
            strm.Read(b, 0, b.Length)
            Select Case b(0) ' First char of FileName
                Case 0 : Exit Do
                Case 5 : b(0) = &HE5
            End Select
            pb = GCHandle.Alloc(b, GCHandleType.Pinned)
            ReDim Preserve des(dirEntryCount)
            des(dirEntryCount) = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(FAT12_16.DirectoryEntry))
            'mRootDirectoryEntries(i)(dirEntryCount) = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(FAT12_16.DirectoryEntry))
            pb.Free()

            dirEntryCount += 1
        Loop

        Return des
    End Function

    Private Function ClusterToSector(partitionNumber As Integer, clusterIndex As Integer) As Long
        Dim rootDirectoryRegionStart As Long = fatRegionStart(partitionNumber) + mBootSectors(partitionNumber).BIOSParameterBlock.NumberOfFATCopies * mBootSectors(partitionNumber).BIOSParameterBlock.SectorsPerFAT * mBootSectors(partitionNumber).BIOSParameterBlock.BytesPerSector
        Dim dataRegionStart As Long = rootDirectoryRegionStart + mBootSectors(partitionNumber).BIOSParameterBlock.RootEntries * 32
        Return dataRegionStart + (clusterIndex - 2) * mBootSectors(partitionNumber).BIOSParameterBlock.SectorsPerCluster * mBootSectors(partitionNumber).BIOSParameterBlock.BytesPerSector
    End Function

    Private Function ReadFile(partitionNumber As Integer, de As FAT12_16.DirectoryEntry) As Byte()
        Dim bytesInCluster As UInt16 = mBootSectors(partitionNumber).BIOSParameterBlock.SectorsPerCluster * mBootSectors(partitionNumber).BIOSParameterBlock.BytesPerSector
        Dim clustersInFile As UInt16 = Math.Ceiling(de.FileSize / bytesInCluster)
        Dim b(clustersInFile * bytesInCluster - 1) As Byte
        Dim clusterIndex As UInt16 = de.StartingCluster
        Dim bytesRead As UInt32

        While clusterIndex < &HFFF8
            strm.Position = ClusterToSector(partitionNumber, clusterIndex)

            Do
                b(bytesRead) = strm.ReadByte()
                bytesRead += 1

                If bytesRead Mod bytesInCluster = 0 Then
                    clusterIndex = mFATDataPointers(partitionNumber)(clusterIndex)
                    Exit Do
                End If
            Loop
        End While

        ReDim Preserve b(de.FileSize - 1)
        Return b
    End Function

    Public ReadOnly Property MasterBootRecord As MBR
        Get
            Return mMasterBootRecord
        End Get
    End Property

    Public ReadOnly Property BootSector(partitionIndex As Integer) As FAT12_16.BootSector
        Get
            Return mBootSectors(partitionIndex)
        End Get
    End Property

    Public ReadOnly Property IsClean(partitionIndex As Integer) As Boolean
        Get
            Return mFATIsClean(partitionIndex)
        End Get
    End Property

    Public ReadOnly Property ReadWriteError(partitionIndex As Integer) As Boolean
        Get
            Return mFATReadWriteError(partitionIndex)
        End Get
    End Property

    Public ReadOnly Property RootDirectoryEntries(partitionIndex As Integer) As FAT12_16.DirectoryEntry()
        Get
            Return mRootDirectoryEntries(partitionIndex)
        End Get
    End Property
End Class
