Imports System.Runtime.InteropServices

' http://www.maverick-os.dk/Mainmenu_NoFrames.html
' http://www.patersontech.com/dos/byte%E2%80%93inside-look.aspx
' https://506889e3-a-62cb3a1a-s-sites.googlegroups.com/site/pcdosretro/fatgen.pdf?attachauth=ANoY7cr_oPcwxUv1I3jB9eaDf2z368nLQUQBc6_zKTfZw8FAs47xAK7Mf3btR_bQEpE5UwDLFgDJrTMovoZOrlC4Eg2qMn935KsT6IAvl5GxhoO_fqmzH7lcAY-7u9y-pbrUKVweCor3XkJPcSg1p-c7COBrRPjHhCgmAIJz1KCZ0iDBzxeE-pGWJ7gbj9-51DovkOLBzmYEcdJVH8xGIwGR_qufNhUuvQ%3D%3D&attredirects=0

Public Class StandardDiskFormat
    Private ReadOnly GeometryTable(,) As Integer = {
        {40, 1, 8, 160 * 1024},
        {40, 2, 8, 320 * 1024},
        {40, 1, 9, 180 * 1024},
        {40, 2, 9, 360 * 1024},
        {80, 2, 9, 720 * 1024},
        {80, 2, 15, 1200 * 1024},
        {80, 2, 18, 1440 * 1024},
        {80, 2, 36, 2880 * 1024}}

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
        Public Signature As UInt16

        Public ReadOnly Property IsBootable As Boolean
            Get
                Return Signature = &HAA55
            End Get
        End Property
    End Structure

    Private mMasterBootRecord As MBR
    Private ReadOnly mBootSectors(4 - 1) As Object ' FAT12.BootSector
    Private ReadOnly mFATDataPointers(4 - 1)() As UInt16
    Private ReadOnly mRootDirectoryEntries(4 - 1)() As Object ' FAT12.DirectoryEntry

    Private ReadOnly strm As IO.Stream
    Private ReadOnly FATRegionStart(4 - 1) As Long

    Public Sub New(s As IO.Stream)
        Dim pb As GCHandle
        Dim b(512 - 1) As Byte

        strm = s

        ' Assume Floppy Image (No partitions)
        ' FIXME: There has to be a better way to know if the image is a floppy or a hard disk
        '        Perhaps some better way to detect if the image has a master boot record or something...
        strm.Position = 0
        strm.Read(b, 0, b.Length)
        pb = GCHandle.Alloc(b, GCHandleType.Pinned)
        Dim bs As FAT12.BootSector = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(FAT12.BootSector))
        pb.Free()
        If bs.BIOSParameterBlock.BytesPerSector = 512 Then
            LoadAsFloppyImage()
        Else
            LoadAsHardDiskImage()
        End If
    End Sub

    Private Sub LoadAsFloppyImage()
        Dim pb As GCHandle
        Dim b(512 - 1) As Byte

        strm.Position = 0

        ReDim mMasterBootRecord.Partitions(0)
        mMasterBootRecord.Partitions(0) = New Partition With {.BootIndicator = BootIndicators.SystemPartition}

        For i As Integer = 0 To GeometryTable.Length / 4 - 1
            If strm.Length = GeometryTable(i, 3) Then
                mMasterBootRecord.Partitions(0).EndingSectorCylinder = ((GeometryTable(i, 0) And &H3FC) << 8) Or
                                                                       ((GeometryTable(i, 0) And &H3) << 6) Or
                                                                        GeometryTable(i, 2)
                mMasterBootRecord.Partitions(0).EndingHead = GeometryTable(i, 1)
                Exit For
            End If
        Next

        strm.Position = 0
        strm.Read(b, 0, b.Length)

        pb = GCHandle.Alloc(b, GCHandleType.Pinned)
        mBootSectors(0) = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(FAT12.BootSector))
        pb.Free()

        Select Case mBootSectors(0).ExtendedBIOSParameterBlock.FileSystemType
            Case "FAT12" : mMasterBootRecord.Partitions(0).SystemId = SystemIds.FAT_12
            Case "FAT16" : mMasterBootRecord.Partitions(0).SystemId = SystemIds.FAT_16
            Case Else : mMasterBootRecord.Partitions(0).SystemId = SystemIds.EMPTY
        End Select

        mMasterBootRecord.Signature = mBootSectors(0).Signature

        ReadFAT(0)
    End Sub

    Private Sub LoadAsHardDiskImage()
        Dim pb As GCHandle
        Dim b(512 - 1) As Byte

        strm.Position = 0
        strm.Read(b, 0, b.Length)
        pb = GCHandle.Alloc(b, GCHandleType.Pinned)
        mMasterBootRecord = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(MBR))
        pb.Free()

        For partitionNumber As Integer = 0 To 4 - 1
            strm.Position = mMasterBootRecord.Partitions(partitionNumber).RelativeSector * 512
            strm.Read(b, 0, b.Length)
            pb = GCHandle.Alloc(b, GCHandleType.Pinned)

            Select Case mMasterBootRecord.Partitions(partitionNumber).SystemId
                Case StandardDiskFormat.SystemIds.FAT_12
                    mBootSectors(partitionNumber) = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(FAT12.BootSector))
                Case StandardDiskFormat.SystemIds.FAT_16
                    mBootSectors(partitionNumber) = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(FAT16.BootSector))
                Case StandardDiskFormat.SystemIds.FAT_BIGDOS
                    mBootSectors(partitionNumber) = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(FAT32.BootSector))
                Case SystemIds.EMPTY
                    pb.Free()
                    Continue For
            End Select

            pb.Free()
            ReadFAT(partitionNumber)
        Next
    End Sub

    Private Sub ReadFAT(partitionNumber As Integer)
        FATRegionStart(partitionNumber) = strm.Position

        ReDim mFATDataPointers(partitionNumber)(CUInt(mBootSectors(partitionNumber).BIOSParameterBlock.SectorsPerFAT) * mBootSectors(partitionNumber).BIOSParameterBlock.BytesPerSector / 2 - 1)
        For j As Integer = 0 To mFATDataPointers(partitionNumber).Length - 1
            Select Case mMasterBootRecord.Partitions(partitionNumber).SystemId ' mBootSectors(partitionNumber).ExtendedBIOSParameterBlock.FileSystemType
                Case StandardDiskFormat.SystemIds.FAT_12 ' TODO: There has to be a simpler way to parse all 12bit addresses
                    'If strm.Position = 668 Then Stop
                    Dim b1 As Byte = strm.ReadByte()
                    Dim b2 As Byte = strm.ReadByte()
                    Dim n As UInt16

                    If j = 0 Then
                        n = BitConverter.ToUInt16({b1, b2}, 0)
                    Else
                        If b1 = &HFF AndAlso b2 = &HF Then
                            n = &HFFFF
                        Else
                            If j Mod 2 = 0 Then
                                b2 = b2 And &HF
                            Else
                                b1 = (b1 And &HF0) >> 4
                            End If

                            n = b1 + (b2 << 12)
                        End If
                    End If

                    mFATDataPointers(partitionNumber)(j) = n
                    If j Mod 2 = 0 Then strm.Position -= 1
                Case StandardDiskFormat.SystemIds.FAT_16
                    mFATDataPointers(partitionNumber)(j) = BitConverter.ToUInt16({strm.ReadByte(), strm.ReadByte()}, 0)
                Case StandardDiskFormat.SystemIds.FAT_BIGDOS
                    mFATDataPointers(partitionNumber)(j) = BitConverter.ToUInt16({strm.ReadByte(), strm.ReadByte()}, 0)
            End Select
        Next

        If (mFATDataPointers(partitionNumber)(0) And &HFF) = mBootSectors(partitionNumber).BIOSParameterBlock.MediaDescriptor Then
            mRootDirectoryEntries(partitionNumber) = GetDirectoryEntries(partitionNumber, -1)
        Else
            ' Invalid boot sector
        End If
    End Sub

    Public Function GetDirectoryEntries(partitionNumber As Integer, Optional clusterIndex As Integer = -1) As Object() ' FAT12.DirectoryEntry()
        Dim bytesInCluster As UInt16 = mBootSectors(partitionNumber).BIOSParameterBlock.SectorsPerCluster * mBootSectors(partitionNumber).BIOSParameterBlock.BytesPerSector
        Dim pb As GCHandle
        Dim des() As Object = Nothing ' FAT12.DirectoryEntry = Nothing
        Dim b(32 - 1) As Byte
        Dim bytesRead As UInt32
        Dim dirEntryCount As Integer = -1
        Dim de As Object = Nothing

        While clusterIndex < &HFFF8
            If clusterIndex = -1 Then
                strm.Position = FATRegionStart(partitionNumber) + mBootSectors(partitionNumber).BIOSParameterBlock.NumberOfFATCopies * mFATDataPointers(partitionNumber).Length * 2
            Else
                strm.Position = ClusterIndexToSector(partitionNumber, clusterIndex)
            End If

            Do
                strm.Read(b, 0, b.Length)
                Select Case b(0) ' First char of FileName
                    Case 0 : clusterIndex = -1 : Exit Do
                    Case 5 : b(0) = &HE5
                End Select
                pb = GCHandle.Alloc(b, GCHandleType.Pinned)
                Select Case mMasterBootRecord.Partitions(partitionNumber).SystemId
                    Case SystemIds.FAT_12, SystemIds.FAT_16
                        de = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(FAT12.DirectoryEntry))
                    Case SystemIds.FAT_BIGDOS
                        de = Marshal.PtrToStructure(pb.AddrOfPinnedObject(), GetType(FAT32.DirectoryEntry))
                End Select
                pb.Free()

                If de.StartingClusterValue > 0 Then
                    dirEntryCount += 1
                    ReDim Preserve des(dirEntryCount)
                    des(dirEntryCount) = de
                End If

                If clusterIndex <> -1 Then
                    bytesRead += b.Length
                    If bytesRead Mod bytesInCluster = 0 Then
                        clusterIndex = mFATDataPointers(partitionNumber)(clusterIndex)
                        Exit Do
                    End If
                End If
            Loop

            If clusterIndex = -1 Then Exit While
        End While

        Return des
    End Function

    Private Function ClusterIndexToSector(partitionNumber As Integer, clusterIndex As Integer) As Long
        Dim bs As Object = mBootSectors(partitionNumber)
        Dim rootDirectoryRegionStart As Long = FATRegionStart(partitionNumber) + bs.BIOSParameterBlock.NumberOfFATCopies * bs.BIOSParameterBlock.SectorsPerFAT * bs.BIOSParameterBlock.BytesPerSector
        Dim dataRegionStart As Long = rootDirectoryRegionStart + bs.BIOSParameterBlock.MaxRootEntries * 32
        Return dataRegionStart + (clusterIndex - 2) * bs.BIOSParameterBlock.SectorsPerCluster * bs.BIOSParameterBlock.BytesPerSector
    End Function

    Public Function ReadFile(partitionNumber As Integer, de As Object) As Byte()
        Dim bytesInCluster As UInt32 = mBootSectors(partitionNumber).BIOSParameterBlock.SectorsPerCluster * mBootSectors(partitionNumber).BIOSParameterBlock.BytesPerSector
        Dim clustersInFile As UInt32 = Math.Ceiling(de.FileSize / bytesInCluster)
        Dim b(clustersInFile * bytesInCluster - 1) As Byte
        Dim clusterIndex As UInt16 = de.StartingClusterValue
        Dim bytesRead As Long

        While clusterIndex < &HFFF8 AndAlso bytesRead < de.FileSize
            strm.Position = ClusterIndexToSector(partitionNumber, clusterIndex)

            Do
                b(bytesRead) = strm.ReadByte()
                bytesRead += 1

                'If bytesRead >= de.FileSize AndAlso (bytesRead Mod bytesInCluster) = 0 Then
                If (bytesRead Mod bytesInCluster) = 0 Then
                    clusterIndex = mFATDataPointers(partitionNumber)(clusterIndex)
                    Exit Do
                End If
            Loop
        End While

        ReDim Preserve b(de.FileSize - 1)
        Return b
    End Function

    Public Sub WriteFile(partitionNumber As Integer, de As Object, file As IO.FileInfo)
        Dim bytesInCluster As UInt16 = mBootSectors(partitionNumber).BIOSParameterBlock.SectorsPerCluster * mBootSectors(partitionNumber).BIOSParameterBlock.BytesPerSector
        Dim clustersInFile As UInt32
        Dim pb As GCHandle
        Dim b(32 - 1) As Byte
        Dim bytesWritten As UInt32
        Dim clusterIndex As Integer = If(de?.StartingCluster = 0, -1, de.StartingCluster)
        Dim foundEmptyDirectoryEntry As Boolean = False

        Dim FindEmptyCluster = Function()
                                   For i As Integer = 0 To mFATDataPointers(partitionNumber).Length - 1
                                       If mFATDataPointers(partitionNumber)(i) = 0 Then
                                           mFATDataPointers(partitionNumber)(i) = &HFFFF ' Temporarily mark as used (EOF)
                                           Return i
                                       End If
                                   Next
                                   Return -1
                               End Function

        ' Find empty directory entry
        While clusterIndex < &HFFF8
            If clusterIndex = -1 Then
                strm.Position = FATRegionStart(partitionNumber) + mBootSectors(partitionNumber).BIOSParameterBlock.NumberOfFATCopies * mFATDataPointers(partitionNumber).Length * 2
            Else
                strm.Position = ClusterIndexToSector(partitionNumber, clusterIndex)
            End If

            Do
                strm.Read(b, 0, b.Length)
                If b(0) = 0 OrElse b(0) = 5 OrElse b(0) = &HE5 Then
                    foundEmptyDirectoryEntry = True
                    Exit While
                End If

                If clusterIndex <> -1 Then
                    bytesWritten += b.Length
                    If bytesWritten Mod bytesInCluster = 0 Then
                        clusterIndex = mFATDataPointers(partitionNumber)(clusterIndex)
                        Exit Do
                    End If
                End If
            Loop

            If clusterIndex = -1 Then Exit While
        End While

        Dim ndeObj As Object = Nothing
        If foundEmptyDirectoryEntry Then
            ' Write Directory Entry
            strm.Position -= b.Length
            pb = GCHandle.Alloc(b, GCHandleType.Pinned)
            Select Case mMasterBootRecord.Partitions(partitionNumber).SystemId
                Case SystemIds.FAT_12, SystemIds.FAT_16
                    Dim nde As New FAT12.DirectoryEntry With {
                        .FileName = If(file.Extension <> "", file.Name.Replace(file.Extension, ""), file.Name),
                        .FileExtension = file.Extension.TrimStart("."c),
                        .Attribute = FAT12.EntryAttributes.ArchiveFlag,
                        .StartingCluster = FindEmptyCluster(),
                        .FileSize = file.Length,
                        .Creation = 0 ' FIXME: Is this correct?
                    }
                    nde.SetTimeDate(file.CreationTime, Now, Now)
                    ndeObj = nde

                    Marshal.StructureToPtr(Of FAT12.DirectoryEntry)(nde, pb.AddrOfPinnedObject(), True)
                Case SystemIds.FAT_BIGDOS
                    Dim nde As New FAT32.DirectoryEntry With {
                        .FileName = If(file.Extension <> "", file.Name.Replace(file.Extension, ""), file.Name),
                        .FileExtension = file.Extension.TrimStart("."c),
                        .Attribute = FAT12.EntryAttributes.ArchiveFlag,
                        .StartingCluster = FindEmptyCluster(),
                        .FileSize = file.Length
                    }
                    nde.SetTimeDate(file.CreationTime, Now, Now)
                    ndeObj = nde

                    Marshal.StructureToPtr(Of FAT32.DirectoryEntry)(nde, pb.AddrOfPinnedObject(), True)
            End Select
            pb.Free()

            strm.Write(b, 0, b.Length)

            ' Write File Bytes
            clustersInFile = Math.Ceiling(ndeObj.FileSize / bytesInCluster)
            bytesWritten = 0
            Using src As IO.FileStream = file.OpenRead()
                clusterIndex = ndeObj.StartingCluster
                strm.Position = ClusterIndexToSector(partitionNumber, clusterIndex)
                While bytesWritten < file.Length
                    strm.WriteByte(src.ReadByte())
                    bytesWritten += 1

                    If (bytesWritten Mod bytesInCluster) = 0 Then
                        Dim newClusterIndex As Integer = FindEmptyCluster()
                        mFATDataPointers(partitionNumber)(clusterIndex) = newClusterIndex
                        clusterIndex = newClusterIndex
                        strm.Position = ClusterIndexToSector(partitionNumber, clusterIndex)
                    End If
                End While

                mFATDataPointers(partitionNumber)(clusterIndex) = &HFFFF

                ' Write new FAT table
                For i As Integer = 0 To mBootSectors(partitionNumber).BIOSParameterBlock.NumberOfFATCopies - 1
                    WriteNewFATTable(partitionNumber, FATRegionStart(partitionNumber) + i * mFATDataPointers(partitionNumber).Length * 2)
                Next

                strm.Flush()
                src.Close()
            End Using
        Else
            ' TODO: Throw some error message...
            Stop
        End If
    End Sub

    Private Sub WriteNewFATTable(partitionNumber As Integer, fatTableStart As Long)
        Dim b() As Byte
        Dim nibbles(6 - 1) As UInt16
        Dim f As Boolean = True

        strm.Position = fatTableStart

        For j As Integer = 0 To mFATDataPointers(partitionNumber).Length / 3 - 1
            Select Case mMasterBootRecord.Partitions(partitionNumber).SystemId
                Case StandardDiskFormat.SystemIds.FAT_12
                    b = BitConverter.GetBytes(mFATDataPointers(partitionNumber)(j) And &HFFFF)

                    If f Then
                        nibbles(0) = (b(0) And &HF0) >> 4
                        nibbles(1) = b(0) And &HF
                        nibbles(3) = b(1) And &HF
                    Else
                        nibbles(4) = (b(0) And &HF0) >> 4
                        nibbles(5) = b(0) And &HF
                        nibbles(2) = b(1) And &HF

                        ' Save
                        b(0) = nibbles(0) << 4 Or nibbles(1)
                        b(1) = nibbles(5) << 4 Or nibbles(3)
                        strm.Write(b, 0, 2)

                        ' ------------------------
                        'Dim b2(2 - 1) As Byte
                        'strm.Read(b2, 0, 2)
                        'If b(0) = 239 Then
                        '    b(0) = 255
                        '    b(1) = If(b(1) = 0, 15, 255)
                        'End If
                        'If b2(0) <> b(0) OrElse b2(1) <> b(1) Then Stop
                        'Debug.Write($"{b(0):X2} {b(1):X2} ")
                        ' ------------------------

                        b(0) = nibbles(2) << 4 Or nibbles(4)
                        strm.Write(b, 0, 1)

                        ' ------------------------
                        'strm.Read(b2, 0, 1)
                        'If b2(0) <> b(0) Then Stop
                        'Debug.Write($"{b(0):X2} ")
                        ' ------------------------
                    End If
                    f = Not f
                Case StandardDiskFormat.SystemIds.FAT_16
                    strm.Write(BitConverter.GetBytes(mFATDataPointers(partitionNumber)(j)), 0, 2)
                Case StandardDiskFormat.SystemIds.FAT_BIGDOS
                    strm.Write(BitConverter.GetBytes(mFATDataPointers(partitionNumber)(j)), 0, 2)
            End Select
        Next
    End Sub

    Public ReadOnly Property MasterBootRecord As MBR
        Get
            Return mMasterBootRecord
        End Get
    End Property

    Public ReadOnly Property BootSector(partitionIndex As Integer) As Object
        Get
            Return mBootSectors(partitionIndex)
        End Get
    End Property

    Public ReadOnly Property IsClean(partitionIndex As Integer) As Boolean
        Get
            Return (mFATDataPointers(partitionIndex)(1) And &H8000) <> 0
        End Get
    End Property

    Public ReadOnly Property IsBootable(partitionIndex As Integer) As Boolean
        Get
            Return mMasterBootRecord.Partitions(partitionIndex).BootIndicator = BootIndicators.SystemPartition AndAlso
                    mBootSectors(partitionIndex).Signature = &HAA55
        End Get
    End Property

    Public ReadOnly Property ReadWriteError(partitionIndex As Integer) As Boolean
        Get
            Return (mFATDataPointers(partitionIndex)(1) And &H4000) = 0
        End Get
    End Property

    Public ReadOnly Property RootDirectoryEntries(partitionIndex As Integer) As Object() ' FAT12.DirectoryEntry()
        Get
            Return mRootDirectoryEntries(partitionIndex)
        End Get
    End Property

    Public ReadOnly Property Cylinders(partitionIndex As Integer) As Int16
        Get
            Dim sc As Int16 = mMasterBootRecord.Partitions(partitionIndex).StartingSectorCylinder >> 6
            sc = (sc >> 2) Or ((sc And &H3) << 8)
            Dim ec As Int16 = mMasterBootRecord.Partitions(partitionIndex).EndingSectorCylinder >> 6
            ec = (ec >> 2) Or ((ec And &H3) << 8)
            Return ec - sc + 1
        End Get
    End Property

    Public ReadOnly Property Sectors(partitionIndex As Integer) As Int16
        Get
            Dim ss As Int16 = mMasterBootRecord.Partitions(partitionIndex).StartingSectorCylinder And &H3F
            Dim es As Int16 = mMasterBootRecord.Partitions(partitionIndex).EndingSectorCylinder And &H3F
            Return es - ss
        End Get
    End Property

    Public ReadOnly Property Heads(partitionIndex As Integer) As Int16
        Get
            Return mMasterBootRecord.Partitions(partitionIndex).EndingHead - mMasterBootRecord.Partitions(partitionIndex).StartingHead
        End Get
    End Property
End Class