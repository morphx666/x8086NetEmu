Public Class FAT12_16
    Public Enum EntryAttributes
        [ReadOnly] = 2 ^ 0
        Hidden = 2 ^ 1
        System = 2 ^ 2
        VolumeName = 2 ^ 3
        Directory = 2 ^ 4
        ArchiveFlag = 2 ^ 5
    End Enum

    Public Class BootSector
        Inherits FileSystemObject

        Public ReadOnly JumpCode(3 - 1) As Byte
        Public ReadOnly OemId(8 - 1) As Char

        Public Class ParameterBlock
            Inherits FileSystemObject

            Public ReadOnly BytesPerSector As UInt16
            Public ReadOnly SectorsPerCluster As Byte
            Public ReadOnly ReservedSectors As UInt16
            Public ReadOnly NumberOfFATCopies As Byte
            Public ReadOnly RootEntries As UInt16
            Public ReadOnly TotalSectors As UInt16
            Public ReadOnly MediaDescriptor As Byte
            Public ReadOnly SectorsPerFAT As UInt16
            Public ReadOnly SectorsPerTrack As UInt16
            Public ReadOnly HeadsPerCylinder As UInt16
            Public ReadOnly HiddenSectors As UInt32
            Public ReadOnly TotalSectorsBig As UInt32

            Public Sub New(s As IO.Stream, Optional offset As Long = -1)
                MyBase.New(s, offset)

                BytesPerSector = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
                SectorsPerCluster = s.ReadByte()
                ReservedSectors = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
                NumberOfFATCopies = s.ReadByte
                RootEntries = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
                TotalSectors = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
                MediaDescriptor = s.ReadByte()
                SectorsPerFAT = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
                SectorsPerTrack = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
                HeadsPerCylinder = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
                HiddenSectors = BitConverter.ToInt32({s.ReadByte(), s.ReadByte(), s.ReadByte(), s.ReadByte()}, 0)
                TotalSectorsBig = BitConverter.ToInt32({s.ReadByte(), s.ReadByte(), s.ReadByte(), s.ReadByte()}, 0)
            End Sub
        End Class

        Public Class DirectoryEntry
            Inherits FileSystemObject

            Public Enum EntryTypes
                Valid
                Free
                Last
            End Enum

            Public ReadOnly FileName(8 - 1) As Char
            Public ReadOnly FileExtension(3 - 1) As Char
            Public ReadOnly Attribute As EntryAttributes
            Public ReadOnly ReservedNT As Byte
            Public ReadOnly Creation As Byte
            Public ReadOnly CreationTime As UInt16
            Public ReadOnly CreationDate As UInt16
            Public ReadOnly LastAccessDate As UInt16
            Public ReadOnly ReservedFAT32 As UInt16
            Public ReadOnly LastWriteTime As UInt16
            Public ReadOnly LastWriteDate As UInt16
            Public ReadOnly StartingCluster As UInt16
            Public ReadOnly FileSize As UInt32
            Public ReadOnly EntryType As EntryTypes

            Public Sub New(s As IO.Stream, Optional offset As Long = -1)
                MyBase.New(s, offset)


                Dim b As Byte
                For i As Integer = 0 To FileName.Length - 1
                    b = s.ReadByte()
                    If b = 0 Then
                        EntryType = EntryTypes.Last
                        Exit Sub
                    ElseIf b = &HE5 Then
                        EntryType = EntryTypes.Free
                    ElseIf b = 5 Then
                        b = &HE5
                    End If
                    FileName(i) = Convert.ToChar(b)
                Next
                EntryType = EntryTypes.Valid

                For i As Integer = 0 To FileExtension.Length - 1
                    FileExtension(i) = Convert.ToChar(s.ReadByte())
                Next

                Attribute = s.ReadByte()

                ReservedNT = s.ReadByte()

                Creation = s.ReadByte()
                CreationTime = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
                CreationDate = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)

                LastAccessDate = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)

                ReservedFAT32 = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)

                LastWriteTime = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
                LastWriteDate = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)

                StartingCluster = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)

                FileSize = BitConverter.ToInt32({s.ReadByte(), s.ReadByte(), s.ReadByte(), s.ReadByte()}, 0)
            End Sub

            Public Shadows Function ToString() As String
                Dim fn As String = New String(FileName).TrimEnd()
                Dim fe As String = New String(FileExtension).TrimEnd()
                If fe <> "" Then fe = "." + fe

                Dim attrs() As String = [Enum].GetNames(GetType(EntryAttributes))
                Dim attr As String = ""
                For i As Integer = 0 To attrs.Length - 1
                    If ((2 ^ i) And Attribute) <> 0 Then
                        attr += attrs(i) + " "
                    End If
                Next
                attr = attr.TrimEnd()

                Return $"{fn}{fe} [{attr}]"
            End Function
        End Class

        Public ReadOnly BIOSParameterBlock As ParameterBlock
        Public ReadOnly DriveNumber As Byte
        Public ReadOnly Reserved As Byte
        Public ReadOnly ExtendedBootSignature As Byte
        Public ReadOnly SerialNumber As UInt32
        Public ReadOnly VolumeLabel(11 - 1) As Char
        Public ReadOnly FileSystemType(8 - 1) As Char
        Public ReadOnly BootStrapCode(448 - 1) As Byte
        Public ReadOnly Signature As UInt16
        Public ReadOnly DirectoryEntries As List(Of DirectoryEntry)

        Public ReadOnly IsVolumeClean As Boolean
        Public ReadOnly ReadWriteError As Boolean

        Public ReadOnly DataOffset As Long
        Public ReadOnly FATOffset As Long

        Public Sub New(s As IO.Stream, Optional offset As Long = -1)
            MyBase.New(s, offset)

            DirectoryEntries = New List(Of DirectoryEntry)

            s.Read(JumpCode, 0, JumpCode.Length)

            For i As Integer = 0 To OemId.Length - 1
                OemId(i) = Convert.ToChar(s.ReadByte())
            Next

            BIOSParameterBlock = New ParameterBlock(s)

            DriveNumber = s.ReadByte()
            Reserved = s.ReadByte()
            ExtendedBootSignature = s.ReadByte()
            SerialNumber = BitConverter.ToInt32({s.ReadByte(), s.ReadByte(), s.ReadByte(), s.ReadByte()}, 0)

            For i As Integer = 0 To VolumeLabel.Length - 1
                VolumeLabel(i) = Convert.ToChar(s.ReadByte())
            Next

            For i As Integer = 0 To FileSystemType.Length - 1
                FileSystemType(i) = Convert.ToChar(s.ReadByte())
            Next

            s.Read(BootStrapCode, 0, BootStrapCode.Length)

            Signature = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)

            FATOffset = s.Position

            Dim fatCluster1 As UInt16 = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
            If (fatCluster1 And &HFF) <> BIOSParameterBlock.MediaDescriptor Then
                ' There's something wrong with the volume
                Exit Sub
            End If
            Dim fatCluster2 As UInt16 = BitConverter.ToInt16({s.ReadByte(), s.ReadByte()}, 0)
            If fatCluster2 <> &HFFFF Then
                ' There's something wrong with the volume
                Exit Sub
            End If
            IsVolumeClean = (fatCluster2 And &H8000) <> 0
            ReadWriteError = (fatCluster2 And &H4000) = 0

            Dim DirectoryOffset As Long = FATOffset + BIOSParameterBlock.SectorsPerFAT * BIOSParameterBlock.NumberOfFATCopies * BIOSParameterBlock.BytesPerSector
            s.Position = DirectoryOffset
            Do
                Dim di As New DirectoryEntry(s)
                If di.EntryType = DirectoryEntry.EntryTypes.Last Then Exit Do
                DirectoryEntries.Add(di)
            Loop

            DataOffset = DirectoryOffset + (DirectoryEntries.Count * 32)
        End Sub
    End Class
End Class
