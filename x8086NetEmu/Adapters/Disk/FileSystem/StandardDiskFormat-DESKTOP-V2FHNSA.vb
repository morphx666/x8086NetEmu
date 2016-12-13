Public Class StandardDiskFormat
    Public Enum BootIndicators
        NonBootable = 0
        SystemPartition = &H80
    End Enum

    Public Enum SystemIds
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

    Public Class Partition
        Inherits FileSystemObject

        Public ReadOnly BootIndicator As BootIndicators
        Public ReadOnly StartingHead As Byte               ' FEDCBA9876 543210
        Public ReadOnly StartingSectorCylinder As UInt16   ' cccccccccc ssssss
        Public ReadOnly SystemId As SystemIds
        Public ReadOnly EndingHead As Byte
        Public ReadOnly EndingSectorCylinder As UInt16     ' cccccccccc ssssss
        Public ReadOnly RelativeSector As UInt32
        Public ReadOnly TotalSectors As UInt32
        Public ReadOnly FileSystem As FileSystemObject

        Public ReadOnly Cylinders As Integer
        Public ReadOnly Sectors As Integer
        Public ReadOnly Heads As Integer

        Public Sub New(s As IO.Stream, Optional offset As Long = -1)
            MyBase.New(s, offset)

            BootIndicator = s.ReadByte()
            StartingHead = s.ReadByte()
            StartingSectorCylinder = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
            SystemId = s.ReadByte()
            EndingHead = s.ReadByte()
            EndingSectorCylinder = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
            RelativeSector = BitConverter.ToInt32({s.ReadByte(), s.ReadByte(), s.ReadByte(), s.ReadByte()}, 0)
            TotalSectors = BitConverter.ToInt32({s.ReadByte(), s.ReadByte(), s.ReadByte(), s.ReadByte()}, 0)

            Dim tmpOffset As Long = s.Position
            If SystemId = StandardDiskFormat.SystemIds.FAT_12 OrElse SystemId = StandardDiskFormat.SystemIds.FAT_16 Then
                FileSystem = New FAT12_16.BootSector(s, RelativeSector * 512)
            End If
            s.Position = tmpOffset

            Heads = EndingHead - StartingHead
            Dim startingCylinder As UShort = StartingSectorCylinder >> 6
            startingCylinder = startingCylinder And &H3 << 8 Or ((startingCylinder And &H3FC) >> 2)
            Dim startingSector As UShort = StartingSectorCylinder And &H3F
            Dim endingCylinder As UShort = EndingSectorCylinder >> 6
            endingCylinder = ((endingCylinder And &H3)) << 8 Or ((endingCylinder And &H3FC) >> 2)
            Dim endingSector As UShort = EndingSectorCylinder And &H3F
            Cylinders = endingCylinder - startingCylinder
            Sectors = endingSector - startingSector
        End Sub

        Public Shadows Function ToString() As String
            Return $"{BootIndicator}: {SystemId}"
        End Function
    End Class

    Public Class MasterBootRecord
        Inherits FileSystemObject

        Public ReadOnly BootCode(446 - 1) As Byte
        Public ReadOnly Partitions(4 - 1) As Partition
        Public ReadOnly Signature As UInt16 ' AA55 = bootable

        Public Sub New(s As IO.Stream, Optional offset As Long = -1)
            MyBase.New(s, offset)

            s.Read(BootCode, 0, BootCode.Length)

            For i As Integer = 0 To Partitions.Length - 1
                Partitions(i) = New Partition(s, s.Position)
            Next

            Signature = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
        End Sub
    End Class
End Class
