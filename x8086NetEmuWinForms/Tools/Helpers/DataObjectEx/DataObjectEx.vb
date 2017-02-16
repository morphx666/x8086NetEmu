Imports System.Collections.Generic
Imports System.IO
Imports System.Windows.Forms
Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.ComTypes
Imports System.Security.Permissions
Imports x8086NetEmu

' HUGE kudos to http://stackoverflow.com/questions/1008984/implement-file-dragging-to-the-desktop-from-a-net-winforms-application

Public Class DataObjectEx
    Inherits DataObject
    Implements ComTypes.IDataObject

    Private Shared ReadOnly ALLOWED_TYMEDS As TYMED() = New TYMED() {TYMED.TYMED_HGLOBAL, TYMED.TYMED_ISTREAM, TYMED.TYMED_ENHMF, TYMED.TYMED_MFPICT, TYMED.TYMED_GDI}

    Public Delegate Function ReadFile(selItem As SelectedItem) As Byte()

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
    Private Structure FILEDESCRIPTOR
        Public dwFlags As UInt32
        Public clsid As Guid
        Public sizel As Size
        Public pointl As Point
        Public dwFileAttributes As UInt32
        Public ftCreationTime As ComTypes.FILETIME
        Public ftLastAccessTime As ComTypes.FILETIME
        Public ftLastWriteTime As ComTypes.FILETIME
        Public nFileSizeHigh As UInt32
        Public nFileSizeLow As UInt32
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=260)>
        Public cFileName As [String]
    End Structure

    Public Structure SelectedItem
        Public FileName As String
        Public WriteTime As Date
        Public FileSize As Int64
    End Structure

    Private mSelectedItems As SelectedItem()
    Private mLindex As Int32

    Private fileReader As ReadFile

    Public Sub New(selectedItems As SelectedItem(), fileReader As ReadFile)
        mSelectedItems = selectedItems
        Me.fileReader = fileReader
    End Sub

    Public Overrides Function GetData(format As String, autoConvert As Boolean) As Object
        If String.Compare(format, NativeMethods.CFSTR_FILEDESCRIPTORW, StringComparison.OrdinalIgnoreCase) = 0 AndAlso mSelectedItems IsNot Nothing Then
            MyBase.SetData(NativeMethods.CFSTR_FILEDESCRIPTORW, GetFileDescriptor(mSelectedItems))
        ElseIf String.Compare(format, NativeMethods.CFSTR_FILECONTENTS, StringComparison.OrdinalIgnoreCase) = 0 Then
            MyBase.SetData(NativeMethods.CFSTR_FILECONTENTS, GetFileContents(mSelectedItems, mLindex))
            'TODO: Cleanup routines after paste has been performed
        ElseIf String.Compare(format, NativeMethods.CFSTR_PERFORMEDDROPEFFECT, StringComparison.OrdinalIgnoreCase) = 0 Then
        End If
        Return MyBase.GetData(format, autoConvert)
    End Function

    <SecurityPermission(SecurityAction.Demand, Flags:=SecurityPermissionFlag.UnmanagedCode)>
    Private Overloads Sub GetData(ByRef formatetc As FORMATETC, ByRef medium As STGMEDIUM) Implements ComTypes.IDataObject.GetData
        If formatetc.cfFormat = DataFormats.GetFormat(NativeMethods.CFSTR_FILECONTENTS).Id Then
            mLindex = formatetc.lindex
        End If

        medium = New STGMEDIUM()
        If GetTymedUseable(formatetc.tymed) Then
            If (formatetc.tymed And TYMED.TYMED_HGLOBAL) <> TYMED.TYMED_NULL Then
                medium.tymed = TYMED.TYMED_HGLOBAL
                medium.unionmember = NativeMethods.GlobalAlloc(NativeMethods.GHND Or NativeMethods.GMEM_DDESHARE, 1)
                If medium.unionmember = IntPtr.Zero Then Throw New OutOfMemoryException()
                Try
                    DirectCast(Me, ComTypes.IDataObject).GetDataHere(formatetc, medium)
                    Exit Sub
                Catch ex As Exception
                    NativeMethods.GlobalFree(New HandleRef(medium, medium.unionmember))
                    medium.unionmember = IntPtr.Zero
                    'Throw
                    Exit Sub
                End Try
            End If
            medium.tymed = formatetc.tymed
            DirectCast(Me, ComTypes.IDataObject).GetDataHere(formatetc, medium)
        Else
            Marshal.ThrowExceptionForHR(NativeMethods.DV_E_TYMED)
        End If
    End Sub

    Private Shared Function GetTymedUseable(tymed As TYMED) As Boolean
        For i As Int32 = 0 To ALLOWED_TYMEDS.Length - 1
            If (tymed And ALLOWED_TYMEDS(i)) <> TYMED.TYMED_NULL Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Function GetFileDescriptor(selectedItems As SelectedItem()) As MemoryStream
        Dim FileDescriptorMemoryStream As New MemoryStream()
        ' Write out the FILEGROUPDESCRIPTOR.cItems value
        FileDescriptorMemoryStream.Write(BitConverter.GetBytes(selectedItems.Length), 0, Marshal.SizeOf(GetType(UInt32)))

        Dim FileDescriptor As New FILEDESCRIPTOR()
        For Each si As SelectedItem In selectedItems
            FileDescriptor.cFileName = si.FileName
            Dim FileWriteTimeUtc As Int64 = si.WriteTime.ToFileTimeUtc()
            FileDescriptor.ftLastWriteTime.dwHighDateTime = FileWriteTimeUtc >> 32
            FileDescriptor.ftLastWriteTime.dwLowDateTime = FileWriteTimeUtc And &HFFFFFFFFUI
            FileDescriptor.nFileSizeHigh = si.FileSize >> 32
            FileDescriptor.nFileSizeLow = si.FileSize And &HFFFFFFFFUI
            FileDescriptor.dwFlags = NativeMethods.FD_WRITESTIME Or NativeMethods.FD_FILESIZE Or NativeMethods.FD_PROGRESSUI

            ' Marshal the FileDescriptor structure into a byte array and write it to the MemoryStream.
            Dim FileDescriptorSize As Int32 = Marshal.SizeOf(FileDescriptor)
            Dim FileDescriptorPointer As IntPtr = Marshal.AllocHGlobal(FileDescriptorSize)
            Marshal.StructureToPtr(FileDescriptor, FileDescriptorPointer, True)
            Dim FileDescriptorByteArray As [Byte]() = New [Byte](FileDescriptorSize - 1) {}
            Marshal.Copy(FileDescriptorPointer, FileDescriptorByteArray, 0, FileDescriptorSize)
            Marshal.FreeHGlobal(FileDescriptorPointer)
            FileDescriptorMemoryStream.Write(FileDescriptorByteArray, 0, FileDescriptorByteArray.Length)
        Next
        Return FileDescriptorMemoryStream
    End Function

    Private Function GetFileContents(selectedItems As SelectedItem(), fileNumber As Int32) As MemoryStream
        Dim fileContentMemoryStream As MemoryStream = Nothing
        If selectedItems IsNot Nothing AndAlso fileNumber < selectedItems.Length Then
            fileContentMemoryStream = New MemoryStream()
            Dim si As SelectedItem = selectedItems(fileNumber)

            ' **************************************************************************************
            ' TODO: Get the virtual file contents and place the contents in the byte array bBuffer.
            ' If the contents are zero length then a single byte must be supplied to Windows
            ' Explorer otherwise the transfer will fail.  If this is part of a multi-file transfer,
            ' the entire transfer will fail at this point if the buffer is zero length.
            ' **************************************************************************************

            Dim bBuffer() As Byte = fileReader.Invoke(si)

            If bBuffer?.Length = 0 Then
                ' Must send at least one byte for a zero length file to prevent stoppages.
                bBuffer = New Byte(0) {}
            End If
            fileContentMemoryStream.Write(bBuffer, 0, bBuffer.Length)
        End If
        Return fileContentMemoryStream
    End Function
End Class

Public Class NativeMethods
    <DllImport("kernel32.dll", CharSet:=CharSet.Auto, ExactSpelling:=True)>
    Public Shared Function GlobalAlloc(uFlags As Integer, dwBytes As Integer) As IntPtr
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Auto, ExactSpelling:=True)>
    Public Shared Function GlobalFree(handle As HandleRef) As IntPtr
    End Function

    ' Clipboard formats used for cut/copy/drag operations
    Public Const CFSTR_PREFERREDDROPEFFECT As String = "Preferred DropEffect"
    Public Const CFSTR_PERFORMEDDROPEFFECT As String = "Performed DropEffect"
    Public Const CFSTR_FILEDESCRIPTORW As String = "FileGroupDescriptorW"
    Public Const CFSTR_FILECONTENTS As String = "FileContents"

    ' File Descriptor Flags
    Public Const FD_CLSID As Int32 = &H1
    Public Const FD_SIZEPOINT As Int32 = &H2
    Public Const FD_ATTRIBUTES As Int32 = &H4
    Public Const FD_CREATETIME As Int32 = &H8
    Public Const FD_ACCESSTIME As Int32 = &H10
    Public Const FD_WRITESTIME As Int32 = &H20
    Public Const FD_FILESIZE As Int32 = &H40
    Public Const FD_PROGRESSUI As Int32 = &H4000
    Public Const FD_LINKUI As Int32 = &H8000

    ' Global Memory Flags
    Public Const GMEM_MOVEABLE As Int32 = &H2
    Public Const GMEM_ZEROINIT As Int32 = &H40
    Public Const GHND As Int32 = (GMEM_MOVEABLE Or GMEM_ZEROINIT)
    Public Const GMEM_DDESHARE As Int32 = &H2000

    ' IDataObject constants
    Public Const DV_E_TYMED As Int32 = &H80040069
End Class