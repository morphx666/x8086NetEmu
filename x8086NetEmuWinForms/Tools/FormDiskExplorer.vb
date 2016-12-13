Imports Microsoft.Win32
Imports x8086NetEmu

Public Class FormDiskExplorer
    Private sdf As StandardDiskFormat

    Public Sub Initialize(fileName As String)
        sdf = New StandardDiskFormat(New IO.FileStream(fileName, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite))
        DisplayFileSystem()
    End Sub

    Private Sub DisplayFileSystem()
        Dim directories = From de In sdf.RootDirectoryEntries(0)
                          Where (de.Attribute And FAT12_16.EntryAttributes.Directory) = FAT12_16.EntryAttributes.Directory AndAlso
                                Convert.ToByte(de.FileNameChars(0)) <> &HE5
                          Order By de.FileName
        Dim files = From de In sdf.RootDirectoryEntries(0)
                    Where (de.Attribute And FAT12_16.EntryAttributes.Directory) <> FAT12_16.EntryAttributes.Directory AndAlso
                          (de.Attribute And FAT12_16.EntryAttributes.VolumeName) <> FAT12_16.EntryAttributes.VolumeName AndAlso
                          Convert.ToByte(de.FileNameChars(0)) <> &HE5
                    Order By de.FileName

        For Each d In directories
            With ListViewFileSystem.Items.Add(d.FileName).SubItems
                .Add("")
                '.Add(GetTypeDescription(d.FileExtension))
                .Add("")
                .Add("")
            End With
        Next

        For Each f In files
            With ListViewFileSystem.Items.Add(f.FileName).SubItems
                .Add($"{f.WriteDateTime.ToShortDateString()} {f.WriteDateTime.ToLongTimeString()}")
                .Add(GetTypeDescription(f.FileExtension))
                .Add(f.FileSize.ToString("N0"))
            End With
        Next

        ListViewFileSystem.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)
    End Sub

    Private Function GetTypeDescription(extension As String) As String
        Dim rk1 As RegistryKey = Registry.ClassesRoot.OpenSubKey($".{extension}")
        If rk1 Is Nothing Then
            Return $"{extension} File"
        Else
            Dim v As String = rk1.GetValue("")
            If v Is Nothing Then Return $"{extension} File"

            Dim rk2 As RegistryKey = Registry.ClassesRoot.OpenSubKey(v)
            Return rk2.GetValue("")
        End If
    End Function
End Class