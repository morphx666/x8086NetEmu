Imports Microsoft.Win32
Imports x8086NetEmu

Public Class FormDiskExplorer
    Private sdf As StandardDiskFormat
    Private selectedParitionIndex As Integer
    Private ignoreNextEvent As Boolean
    Private isMouseDown As Boolean

    Public Sub Initialize(fileName As String)
        sdf = New StandardDiskFormat(New IO.FileStream(fileName, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite))

        LabelImageFile.Text = fileName
        ImageListIcons.Images.Add(Win32FileIcon.GetIconFromFile("."))

        InitLVCode()
        AutoSizeLastColumn(ListViewCode)
        ListViewCode.BackColor = Color.FromArgb(34, 40, 42)
        ListViewCode.ForeColor = Color.FromArgb(102, 80, 15)

        LoadPartitions()
    End Sub

    Private Sub LoadPartitions()
        AddHandler ComboBoxPartitions.SelectedIndexChanged, Sub()
                                                                selectedParitionIndex = ComboBoxPartitions.SelectedIndex

                                                                LabelVolumeLabel.Text = sdf.BootSector(selectedParitionIndex).ExtendedBIOSParameterBlock.VolumeLabel
                                                                LabelFileSystem.Text = sdf.BootSector(selectedParitionIndex).ExtendedBIOSParameterBlock.FileSystemType
                                                                LabelOemId.Text = sdf.BootSector(selectedParitionIndex).OemId
                                                                LabelSerialNumber.Text = sdf.BootSector(selectedParitionIndex).ExtendedBIOSParameterBlock.SerialNumber

                                                                DecodeBootStrapCode()

                                                                Dim rootNode As TreeNode
                                                                Dim volLabels As IEnumerable(Of String) = (From de In sdf.RootDirectoryEntries(selectedParitionIndex)
                                                                                                           Where (de.Attribute And FAT12_16.EntryAttributes.VolumeName) = FAT12_16.EntryAttributes.VolumeName
                                                                                                           Select (de.FileName))

                                                                TreeViewDirs.Nodes.Clear()
                                                                If volLabels.Count > 0 Then
                                                                    rootNode = New TreeNode(volLabels.First(), -1, -1)
                                                                Else
                                                                    rootNode = New TreeNode("[No Label]", -1, -1)
                                                                End If
                                                                TreeViewDirs.Nodes.Add(rootNode)

                                                                DisplayFileSystem(rootNode, sdf.RootDirectoryEntries(selectedParitionIndex))
                                                            End Sub

        For i As Integer = 0 To sdf.MasterBootRecord.Partitions.Length - 1
            If sdf.MasterBootRecord.Partitions(i).SystemId = StandardDiskFormat.SystemIds.FAT_12 OrElse sdf.MasterBootRecord.Partitions(i).SystemId = StandardDiskFormat.SystemIds.FAT_16 Then
                ComboBoxPartitions.Items.Add(sdf.MasterBootRecord.Partitions(i).ToString() +
                                             $" {If(sdf.IsBootable(i), "BOOT", "")} [H:{sdf.Heads(i)} C:{sdf.Cylinders(i)} S:{sdf.Sectors(i)}]")
            End If
        Next

        If ComboBoxPartitions.Items.Count > 0 Then ComboBoxPartitions.SelectedIndex = 0
    End Sub

    Private Sub DisplayFileSystem(parentNode As TreeNode, entries() As FAT12_16.DirectoryEntry)
        If entries Is Nothing Then entries = sdf.RootDirectoryEntries(selectedParitionIndex)

        Dim directories = From de In entries
                          Where (de.Attribute And FAT12_16.EntryAttributes.Directory) = FAT12_16.EntryAttributes.Directory AndAlso
                                Convert.ToByte(de.FileNameChars(0)) < &H5E
                          Order By de.FileName
        Dim files = From de In entries
                    Where (de.Attribute And FAT12_16.EntryAttributes.Directory) <> FAT12_16.EntryAttributes.Directory AndAlso
                          (de.Attribute And FAT12_16.EntryAttributes.VolumeName) <> FAT12_16.EntryAttributes.VolumeName AndAlso
                          Convert.ToByte(de.FileNameChars(0)) < &H5E
                    Order By GetTypeDescription(de.FileExtension)

        'Dim driveNumber As Integer = sdf.BootSector(0).DriveNumber
        'Dim rootNode As TreeNode = TreeViewDirs.Nodes.Add(Chr(If(driveNumber >= 128, Asc("C") + driveNumber - 128, Asc("A") + driveNumber)) + ":")
        'Dim volLabels As IEnumerable(Of String) = (From de In entries
        '                                           Where (de.Attribute And FAT12_16.EntryAttributes.VolumeName) = FAT12_16.EntryAttributes.VolumeName
        '                                           Select de.FileName)

        Dim node As TreeNode = Nothing
        ListViewFileSystem.Items.Clear()
        For Each d In directories
            If d.FileName <> "." AndAlso d.FileName <> ".." Then
                node = FindNode(d, parentNode)

                If node Is Nothing Then
                    node = New TreeNode(d.FileName, 0, 0)
                    node.Tag = d
                    parentNode.Nodes.Add(node)
                End If

                With ListViewFileSystem.Items.Add(d.FileName, 0)
                    With .SubItems
                        .Add("")
                        .Add(GetTypeDescription("Directory"))
                        .Add("")
                    End With
                    .Tag = {node, d}
                End With
            End If
        Next

        For Each f In files
            With ListViewFileSystem.Items.Add(f.FileName, GetExtensionIconIndex(f.FileExtension))
                With .SubItems
                    .Add(Math.Ceiling((f.FileSize / 1024)).ToString("N0") + " KB")
                    .Add(GetTypeDescription($".{f.FileExtension}"))
                    .Add($"{f.WriteDateTime.ToShortDateString()} {f.WriteDateTime.ToLongTimeString()}")
                End With
                .Tag = {node, f}
            End With
        Next

        parentNode.Expand()
        TreeViewDirs.SelectedNode = parentNode
        ListViewFileSystem.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)
    End Sub

    Private Function GetExtensionIconIndex(ext As String) As Integer
        ext = "." + ext

        Dim index As Integer = -1
        Dim extKey As RegistryKey = Registry.ClassesRoot.OpenSubKey(ext)

        Dim extValue As String = extKey?.GetValue("")?.ToString()
        Dim dataKey As RegistryKey = Registry.ClassesRoot.OpenSubKey(extValue + "\DefaultIcon")

        If dataKey IsNot Nothing AndAlso dataKey.ValueCount > 0 AndAlso dataKey.GetValue("") IsNot Nothing Then
            Dim dataValue As String = dataKey.GetValue("").ToString()

            index = ImageListIcons.Images.IndexOfKey(extValue)
            If index = -1 Then
                Dim ico As Icon = Win32FileIcon.GetIconFromFile(dataValue)
                If ico IsNot Nothing Then
                    ImageListIcons.Images.Add(extValue, ico)
                    index = ImageListIcons.Images.IndexOfKey(extValue)

                    'ImageListIcons.Images(index).Save($"d:\users\xavier\desktop\{extValue}.png", Imaging.ImageFormat.Png)
                End If
            End If
        End If

        extKey?.Close()
        dataKey?.Close()

        Return index
    End Function

    Private Function FindNode(d As FAT12_16.DirectoryEntry, parentNode As TreeNode) As TreeNode
        For Each n As TreeNode In parentNode.Nodes
            If n.Tag IsNot Nothing AndAlso CType(n.Tag, FAT12_16.DirectoryEntry) = d Then
                Return n
            ElseIf n.Nodes.Count > 0 Then
                n = FindNode(d, n)
                If n?.Tag IsNot Nothing AndAlso CType(n.Tag, FAT12_16.DirectoryEntry) = d Then Return n
            End If
        Next
        Return Nothing
    End Function

    Private Function GetTypeDescription(extension As String) As String
        Dim rk1 As RegistryKey = Registry.ClassesRoot.OpenSubKey(extension)
        If rk1 Is Nothing Then
            Return $"{extension.TrimStart(".")} File"
        Else
            Dim v As String = rk1.GetValue("")
            If v Is Nothing Then Return $"{extension.TrimStart(".")} File"

            Dim rk2 As RegistryKey = Registry.ClassesRoot.OpenSubKey(v)
            Return If(rk2 Is Nothing, v, rk2.GetValue(""))
        End If
    End Function

    Private Sub TreeViewDirs_NodeMouseClick(sender As Object, e As TreeNodeMouseClickEventArgs) Handles TreeViewDirs.NodeMouseClick
        Dim node As TreeNode = e.Node
        If node Is Nothing Then Exit Sub

        Dim entry As FAT12_16.DirectoryEntry = CType(node.Tag, FAT12_16.DirectoryEntry)
        DisplayFileSystem(node, sdf.GetDirectoryEntries(0, If(entry.StartingCluster = 0, -1, entry.StartingCluster)))
    End Sub

    Private Sub DecodeBootStrapCode()
        Dim emu As New X8086(True)
        Dim ins As X8086.Instruction
        Dim address As String
        Dim bsc() As Byte = sdf.BootSector(selectedParitionIndex).BootStrapCode
        Array.Copy(bsc, 0, emu.Memory, 0, bsc.Length)

        For i As Integer = 0 To bsc.Length - 1
            address = X8086.SegmentOffetToAbsolute(0, i).ToString("X")
            ins = emu.Decode(0, i)

            With ListViewCode.Items.Add(address, ins.CS.ToHex(X8086.DataSize.Word, "") + ":" + ins.IP.ToHex(X8086.DataSize.Word, ""), 0)
                With .SubItems
                    .Add(GetBytesString(ins.Bytes))
                    .Add(ins.Mnemonic)
                    If ins.Message = "" Then
                        If ins.Parameter2 = "" Then
                            .Add(ins.Parameter1)
                        Else
                            .Add(ins.Parameter1 + ", " + ins.Parameter2)
                        End If
                    Else
                        .Add(ins.Message)
                    End If
                End With
                .Tag = address
                .UseItemStyleForSubItems = False
                .ForeColor = ListViewCode.ForeColor
                .SubItems(1).ForeColor = Color.FromArgb(88, 81, 64)
                .SubItems(2).ForeColor = Color.FromArgb(97, 175, 99)
                .SubItems(3).ForeColor = Color.FromArgb(35 + 20, 87 + 20, 140 + 20)
            End With
        Next
    End Sub

    Private Function GetBytesString(b() As Byte) As String
        Dim r As String = ""
        If b IsNot Nothing Then
            For i As Integer = 0 To b.Length - 1
                r += b(i).ToHex("") + " "
            Next
        End If
        Return r.Trim()
    End Function

    Private Sub InitLVCode()
        ListViewHelper.EnableDoubleBuffer(ListViewCode)

        With ListViewCode.Items.Add("FFFF:FFFF".Replace("F", " "))
            .SubItems.Add("FF FF FF FF FF FF".Replace("F", " "))
            .SubItems.Add("FFFFFF".Replace("F", " "))
            .SubItems.Add("FFFFFFFFFFFFFFFFFFFF".Replace("F", " "))
        End With

        ListViewCode.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)
        ListViewCode.Items(0).Remove()
    End Sub

    Private Sub AutoSizeLastColumn(lv As ListView)
        Dim w As Integer = lv.ClientSize.Width
        Select Case lv.BorderStyle
            Case BorderStyle.Fixed3D : w -= 4
            Case BorderStyle.FixedSingle : w -= 2
        End Select
        For i As Integer = 0 To lv.Columns.Count - 1
            w -= lv.Columns(i).Width
            If w < 1 Then Exit For
        Next

        lv.Columns(lv.Columns.Count - 1).Width += w
    End Sub

    Private Sub ListViewFileSystem_DoubleClick(sender As Object, e As EventArgs) Handles ListViewFileSystem.DoubleClick
        If ListViewFileSystem.SelectedItems.Count <> 1 Then Exit Sub

        Dim slvi As ListViewItem = ListViewFileSystem.SelectedItems(0)
        If slvi.Tag IsNot Nothing Then ' It's a folder
            Dim objs() As Object = CType(slvi.Tag, Object())
            Dim node As TreeNode = CType(objs(0), TreeNode)
            Dim entry As FAT12_16.DirectoryEntry = CType(objs(1), FAT12_16.DirectoryEntry)
            If (entry.Attribute And FAT12_16.EntryAttributes.Directory) = FAT12_16.EntryAttributes.Directory Then ' It's a directory
                DisplayFileSystem(node, sdf.GetDirectoryEntries(0, entry.StartingCluster))
            Else ' It's a file
                Dim b() As Byte = sdf.ReadFile(selectedParitionIndex, entry)
                Dim targetFileName As String = IO.Path.Combine(IO.Path.GetTempPath(), entry.FullFileName)
                IO.File.WriteAllBytes(targetFileName, b)
                Try
                    Process.Start(targetFileName)
                Catch ex As Exception
                End Try
            End If
        End If
    End Sub

    Private Sub ListViewFileSystem_MouseDown(sender As Object, e As MouseEventArgs) Handles ListViewFileSystem.MouseDown
        isMouseDown = True
    End Sub

    Private Sub ListViewFileSystem_MouseMove(sender As Object, e As MouseEventArgs) Handles ListViewFileSystem.MouseMove
        If isMouseDown Then
            Dim filesCount As Integer = ListViewFileSystem.SelectedItems.Count
            Dim si(filesCount - 1) As DataObjectEx.SelectedItem

            For i As Integer = 0 To ListViewFileSystem.SelectedItems.Count - 1
                Dim objs() As Object = CType(ListViewFileSystem.SelectedItems(i).Tag, Object())
                Dim entry As FAT12_16.DirectoryEntry = CType(objs(1), FAT12_16.DirectoryEntry)

                si(i).FileName = entry.FullFileName
                si(i).WriteTime = entry.WriteDateTime
                si(i).FileSize = entry.FileSize
            Next

            Dim dox As New DataObjectEx(si, Function(selItem As DataObjectEx.SelectedItem) As Byte()
                                                Dim b() As Byte = Nothing
                                                Me.Invoke(New MethodInvoker(Sub()
                                                                                For i As Integer = 0 To ListViewFileSystem.SelectedItems.Count - 1
                                                                                    Dim objs() As Object = CType(ListViewFileSystem.SelectedItems(i).Tag, Object())
                                                                                    Dim entry As FAT12_16.DirectoryEntry = CType(objs(1), FAT12_16.DirectoryEntry)

                                                                                    If selItem.FileName = entry.FullFileName AndAlso
                                                                                        selItem.WriteTime = entry.WriteDateTime AndAlso
                                                                                        selItem.FileSize = entry.FileSize Then
                                                                                        b = sdf.ReadFile(selectedParitionIndex, entry)
                                                                                    End If
                                                                                Next
                                                                            End Sub))

                                                Return b
                                            End Function)
            dox.SetData(NativeMethods.CFSTR_FILEDESCRIPTORW, Nothing)
            dox.SetData(NativeMethods.CFSTR_FILECONTENTS, Nothing)
            dox.SetData(NativeMethods.CFSTR_PERFORMEDDROPEFFECT, Nothing)

            'ListViewFileSystem.DoDragDrop(dox, DragDropEffects.All)
            Clipboard.SetDataObject(dox)
            isMouseDown = False
        End If
    End Sub

    Private Sub ListViewFileSystem_MouseUp(sender As Object, e As MouseEventArgs) Handles ListViewFileSystem.MouseUp
        If Clipboard.ContainsFileDropList() Then SendKeys.Send("^V")
        isMouseDown = False
    End Sub
End Class