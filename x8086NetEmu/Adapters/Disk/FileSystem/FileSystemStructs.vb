﻿Imports System.Runtime.InteropServices

Public Class FAT12
    Public Enum EntryAttributes As Byte
        [ReadOnly] = 2 ^ 0
        Hidden = 2 ^ 1
        System = 2 ^ 2
        VolumeName = 2 ^ 3
        Directory = 2 ^ 4
        ArchiveFlag = 2 ^ 5
    End Enum

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Public Structure ParameterBlock
        Public BytesPerSector As UInt16
        Public SectorsPerCluster As Byte
        Public ReservedSectors As UInt16
        Public NumberOfFATCopies As Byte
        Public MaxRootEntries As UInt16
        Public TotalSectors As UInt16
        Public MediaDescriptor As Byte
        Public SectorsPerFAT As UInt16
        Public SectorsPerTrack As UInt16
        Public HeadsPerCylinder As UInt16
        Public HiddenSectors As UInt32
        Public TotalSectorsBig As UInt32
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi, Pack:=1)>
    Public Structure ExtendedParameterBlock
        Public DriveNumber As Byte
        Public Reserved As Byte
        Public ExtendedBootSignature As Byte
        Public SerialNumber As UInt32
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=11)> Private VolumeLabelChars() As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=8)> Private FileSystemTypeChars() As Byte

        Public Property VolumeLabel As String
            Get
                Return Text.Encoding.ASCII.GetString(VolumeLabelChars).Replace(vbNullChar, "").TrimEnd()
            End Get
            Set(value As String)
                If value.Length > 11 Then value = value.Substring(0, 11)
                VolumeLabelChars = Text.Encoding.ASCII.GetBytes(value.ToUpper().PadRight(11, " "c))
            End Set
        End Property

        Public Property FileSystemType As String
            Get
                Return Text.Encoding.ASCII.GetString(FileSystemTypeChars).TrimEnd()
            End Get
            Set(value As String)
                If value.Length > 8 Then value = value.Substring(0, 8)
                FileSystemTypeChars = Text.Encoding.ASCII.GetBytes(value.ToUpper().PadRight(11, " "c))
            End Set
        End Property
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi, Pack:=1)>
    Public Structure DirectoryEntry
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=8)> Public FileNameChars() As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=3)> Private FileExtensionChars() As Byte
        Public Attribute As EntryAttributes
        Public ReservedNT As Byte
        Public Creation As Byte
        Public CreationTime As UInt16
        Public CreationDate As UInt16
        Public LastAccessDate As UInt16
        Public ReservedFAT32 As UInt16
        Public LastWriteTime As UInt16
        Public LastWriteDate As UInt16
        Public StartingCluster As UInt16
        Public FileSize As UInt32

        Public ReadOnly Property StartingClusterValue As Integer
            Get
                Return StartingCluster
            End Get
        End Property

        Public Property FileName As String
            Get
                Return Text.Encoding.ASCII.GetString(FileNameChars).TrimEnd()
            End Get
            Set(value As String)
                Dim fn As String = value.ToUpper().Replace(" "c, "_"c)
                If fn.Length > 8 Then fn = fn.Substring(0, 8 - 2) + "~1"
                FileNameChars = Text.Encoding.ASCII.GetBytes(fn.PadRight(8, " "c))
            End Set
        End Property

        Public Property FileExtension As String
            Get
                Return Text.Encoding.ASCII.GetString(FileExtensionChars).TrimEnd()
            End Get
            Set(value As String)
                Dim fe As String = value.ToUpper()
                If fe.Length > 3 Then fe = fe.Substring(0, 3)
                FileExtensionChars = Text.Encoding.ASCII.GetBytes(fe.PadRight(3, " "c))
            End Set
        End Property

        Public ReadOnly Property FullFileName As String
            Get
                Dim fn As String = FileName.TrimEnd()
                Dim fe As String = FileExtension.TrimEnd()
                If fe <> "" Then fe = "." + fe

                Return fn + fe
            End Get
        End Property

        Public Sub SetTimeDate(creation As Date, lastWrite As Date, lastAccess As Date)
            CreationTime = FSTimeFromNative(creation)
            CreationDate = FSDateFromNative(creation)
            LastWriteTime = FSTimeFromNative(lastWrite)
            LastWriteDate = FSDateFromNative(lastWrite)
            LastAccessDate = FSDateFromNative(lastAccess)
        End Sub

        Public ReadOnly Property CreationDateTime As DateTime
            Get
                Dim t() As Integer = FSTimeToNative(CreationTime)
                Dim d() As Integer = FSDateToNative(CreationTime)
                Return New DateTime(d(2), d(1), d(0), t(2), t(1), t(0))
            End Get
        End Property

        Public ReadOnly Property WriteDateTime As DateTime
            Get
                Dim t() As Integer = FSTimeToNative(LastWriteTime)
                Dim d() As Integer = FSDateToNative(LastWriteDate)
                Try
                    Return New DateTime(d(0), d(1), d(2), t(0), t(1), t(2))
                Catch
                    Return New DateTime(1980, 1, 1, 0, 0, 0)
                End Try
            End Get
        End Property

        Public Shadows Function ToString() As String
            Dim attrs() As String = [Enum].GetNames(GetType(EntryAttributes))
            Dim attr As String = ""
            For i As Integer = 0 To attrs.Length - 1
                If ((2 ^ i) And Attribute) <> 0 Then
                    attr += attrs(i) + " "
                End If
            Next
            attr = attr.TrimEnd()

            Return $"{FullFileName} [{attr}]"
        End Function

        Public Shared Operator =(d1 As DirectoryEntry, d2 As DirectoryEntry) As Boolean
            Return d1.Attribute = d2.Attribute AndAlso d1.StartingClusterValue = d2.StartingClusterValue
        End Operator

        Public Shared Operator <>(d1 As DirectoryEntry, d2 As DirectoryEntry) As Boolean
            Return Not d1 = d2
        End Operator

        Private Function FSTimeToNative(v As UInt16) As Integer()
            Dim s As Integer = (v And &H1F) * 2
            Dim m As Integer = (v And &H3E0) >> 5
            Dim h As Integer = (v And &HF800) >> 11
            Return {h, m, s}
        End Function

        Private Function FSTimeFromNative(v As Date) As UInt16
            Dim h As Integer = (v.Hour << 11) And &HF800
            Dim m As Integer = (v.Minute << 5) And &H3E0
            Dim s As Integer = (v.Second / 2) And &H1F
            Return h Or m Or s
        End Function

        Private Function FSDateToNative(v As UInt16) As Integer()
            Dim d As Integer = v And &H1F
            Dim m As Integer = (v And &H1E0) >> 5
            Dim y As Integer = ((v And &HFE00) >> 9) + 1980
            Return {y, m, d}
        End Function

        Private Function FSDateFromNative(v As Date) As UInt16
            Dim d As Integer = v.Day And &H1F
            Dim m As Integer = (v.Month << 5) And &H1E0
            Dim y As Integer = (v.Year << 9) And &HFE00 + 1980
            Return y Or m Or d
        End Function
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi, Pack:=1)>
    Public Structure BootSector
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=3)> Public JumpCode() As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=8)> Private OemIdChars() As Byte
        <MarshalAs(UnmanagedType.Struct)> Public BIOSParameterBlock As ParameterBlock
        <MarshalAs(UnmanagedType.Struct)> Public ExtendedBIOSParameterBlock As ExtendedParameterBlock
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=448)> Public BootStrapCode() As Byte
        Public Signature As UInt16

        Public Property OemId As String
            Get
                Return Text.Encoding.ASCII.GetString(OemIdChars).TrimEnd()
            End Get
            Set(value As String)
                If value.Length > 8 Then value = value.Substring(0, 8)
                OemIdChars = Text.Encoding.ASCII.GetBytes(value.ToUpper().PadRight(8, " "c))
            End Set
        End Property
    End Structure
End Class

Public Class FAT16
    Inherits FAT12
End Class

Public Class FAT32
    Inherits FAT12

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Public Shadows Structure ParameterBlock
        Public BytesPerSector As UInt16
        Public SectorsPerCluster As Byte
        Public ReservedSectors As UInt16
        Public NumberOfFATCopies As Byte
        Public MaxRootEntries As UInt16 ' Not used
        Public TotalSectors As UInt16 ' Not used
        Public MediaDescriptor As Byte
        Public SectorsPerFAT As UInt16
        Public SectorsPerTrack As UInt16
        Public HeadsPerCylinder As UInt16
        Public HiddenSectors As UInt32
        Public SectorsInPartition As UInt32
        Public SectorsPerFATNew As UInt32
        Public FATHandlingFlags As UInt16
        Public Version As UInt16
        Public ClusterStartRootDirectory As UInt32
        Public SectorStartFileSystemInformationSector As UInt16
        Public SectorStartBackupBootSector As UInt16
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=12)> Private ReadOnly Reserved() As Byte
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi, Pack:=1)>
    Public Shadows Structure DirectoryEntry
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=8)> Public FileNameChars() As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=3)> Private FileExtensionChars() As Byte
        Public Attribute As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=8)> Public Unused01() As Byte
        Public StartingCluster As UInt16
        Public CreationTime As UInt16
        Public CreationDate As UInt16
        Public StartingClusterLowWord As UInt16
        Public FileSize As UInt32

        Public ReadOnly Property StartingClusterValue As Integer
            Get
                Return (StartingCluster << 8) Or StartingClusterLowWord
            End Get
        End Property

        Public Property LastWriteTime As UInt16
            Get
                Return CreationTime
            End Get
            Set(value As UInt16)
                CreationTime = value
            End Set
        End Property

        Public Property LastWriteDate As UInt16
            Get
                Return CreationDate
            End Get
            Set(value As UInt16)
                CreationDate = value
            End Set
        End Property

        Public Property FileName As String
            Get
                Return Text.Encoding.ASCII.GetString(FileNameChars).TrimEnd()
            End Get
            Set(value As String)
                Dim fn As String = value.ToUpper().Replace(" "c, "_"c)
                If fn.Length > 8 Then fn = fn.Substring(0, 8 - 2) + "~1"
                FileNameChars = Text.Encoding.ASCII.GetBytes(fn.PadRight(8, " "c))
            End Set
        End Property

        Public Property FileExtension As String
            Get
                Return Text.Encoding.ASCII.GetString(FileExtensionChars).TrimEnd()
            End Get
            Set(value As String)
                Dim fe As String = value.ToUpper()
                If fe.Length > 3 Then fe = fe.Substring(0, 3)
                FileExtensionChars = Text.Encoding.ASCII.GetBytes(fe.PadRight(3, " "c))
            End Set
        End Property

        Public ReadOnly Property FullFileName As String
            Get
                Dim fn As String = FileName.TrimEnd()
                Dim fe As String = FileExtension.TrimEnd()
                If fe <> "" Then fe = "." + fe

                Return fn + fe
            End Get
        End Property

        Public Sub SetTimeDate(creation As Date, lastWrite As Date, lastAccess As Date)
            CreationTime = FSTimeFromNative(creation)
            CreationDate = FSDateFromNative(creation)
            LastWriteTime = FSTimeFromNative(lastWrite)
            LastWriteDate = FSDateFromNative(lastWrite)
        End Sub

        Public ReadOnly Property CreationDateTime As DateTime
            Get
                Dim t() As Integer = FSTimeToNative(CreationTime)
                Dim d() As Integer = FSDateToNative(CreationTime)
                Return New DateTime(d(2), d(1), d(0), t(2), t(1), t(0))
            End Get
        End Property

        Public ReadOnly Property WriteDateTime As DateTime
            Get
                Dim t() As Integer = FSTimeToNative(LastWriteTime)
                Dim d() As Integer = FSDateToNative(LastWriteDate)

                Try
                    Return New DateTime(d(0), d(1), d(2), t(0), t(1), t(2))
                Catch
                    Return New DateTime(1980, 1, 1, 0, 0, 0)
                End Try
            End Get
        End Property

        Public Shadows Function ToString() As String
            Dim attrs() As String = [Enum].GetNames(GetType(EntryAttributes))
            Dim attr As String = ""
            For i As Integer = 0 To attrs.Length - 1
                If ((2 ^ i) And Attribute) <> 0 Then
                    attr += attrs(i) + " "
                End If
            Next
            attr = attr.TrimEnd()

            Return $"{FullFileName} [{attr}]"
        End Function

        Public Shared Operator =(d1 As DirectoryEntry, d2 As DirectoryEntry) As Boolean
            Return d1.Attribute = d2.Attribute AndAlso d1.StartingClusterValue = d2.StartingClusterValue
        End Operator

        Public Shared Operator <>(d1 As DirectoryEntry, d2 As DirectoryEntry) As Boolean
            Return Not d1 = d2
        End Operator

        Private Function FSTimeToNative(v As UInt16) As Integer()
            Dim s As Integer = (v And &H1F) * 2
            Dim m As Integer = (v And &H3E0) >> 5
            Dim h As Integer = (v And &HF800) >> 11
            Return {h, m, s}
        End Function

        Private Function FSTimeFromNative(v As Date) As UInt16
            Dim h As Integer = (v.Hour << 11) And &HF800
            Dim m As Integer = (v.Minute << 5) And &H3E0
            Dim s As Integer = (v.Second / 2) And &H1F
            Return h Or m Or s
        End Function

        Private Function FSDateToNative(v As UInt16) As Integer()
            Dim d As Integer = v And &H1F
            Dim m As Integer = (v And &H1E0) >> 5
            Dim y As Integer = ((v And &HFE00) >> 9) + 1980
            Return {y, m, d}
        End Function

        Private Function FSDateFromNative(v As Date) As UInt16
            Dim d As Integer = v.Day And &H1F
            Dim m As Integer = (v.Month << 5) And &H1E0
            Dim y As Integer = (v.Year << 9) And &HFE00 + 1980
            Return y Or m Or d
        End Function
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi, Pack:=1)>
    Public Shadows Structure BootSector
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=3)> Public JumpCode() As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=8)> Private ReadOnly OemIdChars() As Byte
        <MarshalAs(UnmanagedType.Struct)> Public BIOSParameterBlock As ParameterBlock
        <MarshalAs(UnmanagedType.Struct)> Public ExtendedBIOSParameterBlock As ExtendedParameterBlock
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=420)> Public BootStrapCode() As Byte
        Public Signature As UInt16

        Public ReadOnly Property OemId As String
            Get
                Return Text.Encoding.ASCII.GetString(OemIdChars).TrimEnd()
            End Get
        End Property
    End Structure
End Class