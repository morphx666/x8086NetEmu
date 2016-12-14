Imports System.Runtime.InteropServices

Public Class Win32FileIcon
    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
    Private Structure SHFILEINFO
        Public hIcon As IntPtr ' : icon
        Public iIcon As Integer ' : icondex
        Public dwAttributes As Integer ' : SFGAO_ flags
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=260)> Public szDisplayName As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=80)> Public szTypeName As String
    End Structure

    Private Declare Ansi Function SHGetFileInfo Lib "shell32.dll" (ByVal pszPath As String, ByVal dwFileAttributes As Integer, ByRef psfi As SHFILEINFO, ByVal cbFileInfo As Integer, ByVal uFlags As Integer) As IntPtr
    Private Const SHGFI_ICON As Integer = &H100
    Private Const SHGFI_SMALLICON As Integer = &H1
    Private Const SHGFI_LARGEICON As Integer = &H0 ' Large icon

    Public Shared Function GetIconFromFile(fileName As String, Optional index As Integer = 0, Optional extractSmallIcon As Boolean = True) As Icon
        Dim hImgSmall As IntPtr  'The handle to the system image list.
        Dim hImgLarge As IntPtr  'The handle to the system image list.
        Dim shinfo As SHFILEINFO = New SHFILEINFO()

        fileName = fileName.Replace("""", "")
        If fileName.Contains(","c) Then
            If Integer.TryParse(fileName.Split(","c)(1), index) Then
                fileName = fileName.Split(","c)(0)
            End If
        End If

        If IO.File.Exists(fileName) OrElse IO.Directory.Exists(fileName) Then
            shinfo.szDisplayName = New String(Chr(0), 260)
            shinfo.szTypeName = New String(Chr(0), 80)
            shinfo.iIcon = index

            If extractSmallIcon Then
                hImgSmall = SHGetFileInfo(fileName, 0, shinfo, Marshal.SizeOf(shinfo), SHGFI_ICON Or SHGFI_SMALLICON)
            Else
                hImgLarge = SHGetFileInfo(fileName, 0, shinfo, Marshal.SizeOf(shinfo), SHGFI_ICON Or SHGFI_LARGEICON)
            End If
            If shinfo.hIcon.ToInt32() = 0 Then
                Return Nothing
            Else
                Return Icon.FromHandle(shinfo.hIcon)
            End If
        End If

        Return Nothing
    End Function
End Class
