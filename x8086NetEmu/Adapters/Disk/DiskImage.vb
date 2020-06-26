' Reverse-Engineering DOS 1.0
' http://www.pagetable.com/?p=165
' IBMBIO: http://www.pagetable.com/?p=184

Public Class DiskImage
    Public Const EOF = -1
    Public Const EIO = -2

    Public Enum ImageStatus
        NoDisk
        DiskLoaded
        DiskImageNotFound
        UnsupportedImageFormat
    End Enum

    Public Enum DriveTypes
        Dt360k = 1
        Dt12M = 2
        Dt720K = 3
        Dt144M = 4
    End Enum

    Protected imgDataStrm As IO.FileStream
    Protected mCylinders As UInt16
    Protected mHeads As UInt16
    Protected mSectors As UInt16
    Protected mSectorSize As UInt16
    Protected mReadOnly As Boolean
    Protected mStatus As ImageStatus = ImageStatus.NoDisk
    Protected mFileLength As ULong
    Protected mIsHardDisk As Boolean
    Protected mFileName As String
    Protected mDriveType As DriveTypes

    Protected Shared mHardDiskCount As Integer

    Public Sub New()
        mCylinders = -1
        mHeads = -1
        mSectors = -1
        mFileLength = -1
    End Sub

    Public ReadOnly Property IsReadOnly As Boolean
        Get
            Return mReadOnly
        End Get
    End Property

    Public ReadOnly Property Status As ImageStatus
        Get
            Return mStatus
        End Get
    End Property

    Public ReadOnly Property IsHardDisk As Boolean
        Get
            Return mIsHardDisk
        End Get
    End Property

    Public ReadOnly Property FileName As String
        Get
            Return mFileName
        End Get
    End Property

    Public Shared ReadOnly Property HardDiskCount As Integer
        Get
            Return mHardDiskCount
        End Get
    End Property

    Public Sub New(fileName As String, Optional mountInReadOnlyMode As Boolean = False, Optional isHardDisk As Boolean = False)
        MyBase.New()

        mFileName = X8086.FixPath(fileName)

        If mReadOnly Then
            imgDataStrm = New IO.FileStream(mFileName, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
        Else
            imgDataStrm = New IO.FileStream(mFileName, IO.FileMode.Open, IO.FileAccess.ReadWrite, IO.FileShare.ReadWrite)
        End If
        mFileLength = imgDataStrm.Length

        OpenImage(mountInReadOnlyMode, isHardDisk)

        X8086.Notify("DiskImage '{0}': {1}", X8086.NotificationReasons.Info, mFileName, mStatus.ToString())
    End Sub

    Protected Sub OpenImage(mountInReadOnlyMode As Boolean, isHardDisk As Boolean)
        mReadOnly = mountInReadOnlyMode

        imgDataStrm.Position = 0

        Try
            mIsHardDisk = isHardDisk
            If isHardDisk Then mHardDiskCount += 1

            If MatchGeometry() Then
                mStatus = ImageStatus.DiskLoaded
            Else
                mStatus = ImageStatus.UnsupportedImageFormat
            End If
        Catch ex As UnauthorizedAccessException
            If mountInReadOnlyMode Then
                mStatus = ImageStatus.UnsupportedImageFormat
            Else
                OpenImage(True, isHardDisk)
            End If
        Catch ex As Exception
            mStatus = ImageStatus.UnsupportedImageFormat
        End Try
    End Sub

    ' Guess the image's disk geometry based on its size
    Protected Friend Function MatchGeometry() As Boolean
        mSectorSize = 512

        If mIsHardDisk Then
            mStatus = ImageStatus.DiskLoaded

            If MatchGeometryMBR() Then Return True
            If MatchGeometryDOS() Then Return True

            Return False
        Else
            mCylinders = -1
            mHeads = -1
            mSectors = -1

            For i As Integer = 0 To StandardDiskFormat.GeometryTable.Length / 4 - 1
                If mFileLength = StandardDiskFormat.GeometryTable(i, 3) Then
                    mCylinders = StandardDiskFormat.GeometryTable(i, 0)
                    mHeads = StandardDiskFormat.GeometryTable(i, 1)
                    mSectors = StandardDiskFormat.GeometryTable(i, 2)
                    Return True
                End If
            Next

            ' Cheap trick to handle images with garbage at the end of the image file (such as the copyright crap inserted by DiskImage)
            For i As Integer = 0 To StandardDiskFormat.GeometryTable.Length / 4 - 1
                If Math.Abs(mFileLength - StandardDiskFormat.GeometryTable(i, 3)) <= 512 Then
                    mCylinders = StandardDiskFormat.GeometryTable(i, 0)
                    mHeads = StandardDiskFormat.GeometryTable(i, 1)
                    mSectors = StandardDiskFormat.GeometryTable(i, 2)
                    Return True
                End If
            Next

            Return False
        End If
    End Function

    Private Function MatchGeometryDOS() As Boolean
        Dim b(512 - 1) As Byte
        If Read(0, b) <> 0 Then Return False
        If b(510) <> &H55 OrElse b(511) <> &HAA Then Return False

        If (b(11 + 1) << 8) + b(11) <> mSectorSize Then Return False

        Dim h As Integer = (b(26 + 1) << 8) + b(26)
        Dim s As Integer = (b(24 + 1) << 8) + b(24)

        If h = 0 OrElse h > 255 Then Return False
        If s = 0 OrElse s > 255 Then Return False

        Dim c As Integer = mFileLength / (h * s * mSectorSize)

        mCylinders = c
        mSectors = s
        mHeads = h

        Return True
    End Function

    Private Function MatchGeometryMBR() As Boolean
        Dim b(512 - 1) As Byte
        If Read(0, b) <> 0 Then Return False
        If b(510) <> &H55 OrElse b(511) <> &HAA Then Return False

        Dim tc1 As Integer
        Dim th1 As Integer
        Dim ts1 As Integer

        Dim tc2 As Integer
        Dim th2 As Integer
        Dim ts2 As Integer

        Dim c As Integer = 0
        Dim h As Integer = 0
        Dim s As Integer = 0

        Dim p As Integer
        For i As Integer = 0 To 4 - 1
            p = &H1BE + 16 * i

            If (b(p) And &H7F) <> 0 Then Return False

            ' Partition Start
            tc1 = b(p + 3) Or ((b(p + 2) And &HC0) << 2)
            th1 = b(p + 1)
            ts1 = b(p + 2) And &H3F
            h = If(th1 > h, th1, h)
            s = If(ts1 > s, ts1, s)

            ' Partition End
            tc2 = b(p + 7) Or ((b(p + 6) And &HC0) << 2)
            th2 = b(p + 5)
            ts2 = b(p + 6) And &H3F
            h = If(th2 > h, th2, h)
            s = If(ts2 > s, ts2, s)

            If tc2 < tc1 Then
                Return False
            ElseIf tc2 = tc1 Then
                If th2 < th1 Then
                    Return False
                ElseIf th2 = th1 Then
                    If ts2 < ts1 Then
                        Return False
                    End If
                End If
            End If
        Next

        If s = 0 Then Return False

        h += 1
        c = mFileLength / (h * s * mSectorSize)

        mCylinders = c
        mSectors = s
        mHeads = h

        Return True
    End Function

    Public Function LBA(cylinder As UInt32, head As UInt32, sector As UInt32) As UInt32
        If mStatus <> ImageStatus.DiskLoaded Then Return -1

        cylinder = cylinder Or ((sector And &HC0) << 2)
        sector = sector And &H3F

        If cylinder >= mCylinders OrElse sector = 0 OrElse sector > mSectors OrElse head >= mHeads Then Return -1

        Return (((cylinder * mHeads) + head) * mSectors + sector - 1) * mSectorSize
    End Function

    Public Sub Close()
        Try
            If mStatus = ImageStatus.DiskLoaded Then imgDataStrm.Close()
        Catch
        Finally
            mStatus = ImageStatus.NoDisk
        End Try
    End Sub

    Public ReadOnly Property FileLength() As ULong
        Get
            Return mFileLength
        End Get
    End Property

    Public Overridable Function Read(offset As ULong, data() As Byte) As Integer
        If mStatus <> ImageStatus.DiskLoaded Then Return -1

        If offset < 0 OrElse offset + data.Length > mFileLength Then Return EOF

        Try
            imgDataStrm.Seek(offset, IO.SeekOrigin.Begin)
            imgDataStrm.Read(data, 0, data.Length)

            Return 0
        Catch e As Exception
            Return EIO
        End Try
    End Function

    Public Overridable Function Write(offset As ULong, data() As Byte) As Integer
        If mStatus <> ImageStatus.DiskLoaded Then Return -1

        If offset < 0 OrElse offset + data.Length > mFileLength Then Return EOF

        Try
            imgDataStrm.Seek(offset, IO.SeekOrigin.Begin)
            imgDataStrm.Write(data, 0, data.Length)
            Return 0
        Catch e As Exception
            Return EIO
        End Try
    End Function

    Public ReadOnly Property Tracks() As UInt16
        Get
            Return mCylinders
        End Get
    End Property

    Public ReadOnly Property Cylinders() As UInt16
        Get
            Return mCylinders
        End Get
    End Property

    Public ReadOnly Property Heads() As UInt16
        Get
            Return mHeads
        End Get
    End Property

    Public ReadOnly Property Sectors() As UInt16
        Get
            Return mSectors
        End Get
    End Property

    Public ReadOnly Property SectorSize As UInt16
        Get
            Return mSectorSize
        End Get
    End Property
End Class