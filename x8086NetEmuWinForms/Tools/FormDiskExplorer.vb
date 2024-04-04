Imports Microsoft.Win32
Imports x8086NetEmu
Imports x8086NetEmu.StandardDiskFormat

Public Class FormDiskExplorer
    Private sdf As StandardDiskFormat
    Private selectedParitionIndex As Integer
    Private ReadOnly draggedItems As New List(Of String)
    Private isLeftMouseDown As Boolean
    Private mouseDownLocation As Point

    ' Alpha features
    Private showDropWarning As Boolean = True
    Private showDeleteWarning As Boolean = True

    Public Sub Initialize(fileName As String)
        sdf = New StandardDiskFormat(New IO.FileStream(fileName, IO.FileMode.Open, IO.FileAccess.ReadWrite, IO.FileShare.ReadWrite))

        LabelImageFile.Text = fileName
        ImageListIcons.Images.Add(Win32FileIcon.GetIconFromFile("."))

        InitListView(ListViewCode)
        ListViewCode.BackColor = Color.FromArgb(34, 40, 42)
        ListViewCode.ForeColor = Color.FromArgb(102, 80, 15)

        If sdf.MasterBootRecord.Partitions(0).SystemId = SystemIds.EMPTY Then
            DecodeBootStrapCode()
        Else
            LoadPartitions()
        End If
    End Sub

    Private Sub LoadPartitions()
        AddHandler ComboBoxPartitions.SelectedIndexChanged, Sub()
                                                                selectedParitionIndex = ComboBoxPartitions.SelectedIndex

                                                                Dim volName As String = sdf.BootSector(selectedParitionIndex).ExtendedBIOSParameterBlock.VolumeLabel
                                                                volName = If(volName = "", "unlabeled", volName)

                                                                LabelVolumeLabel.Text = volName
                                                                LabelFileSystem.Text = sdf.MasterBootRecord.Partitions(selectedParitionIndex).SystemId.ToString()
                                                                LabelOemId.Text = sdf.BootSector(selectedParitionIndex).OemId
                                                                LabelSerialNumber.Text = sdf.BootSector(selectedParitionIndex).ExtendedBIOSParameterBlock.SerialNumber

                                                                DecodeBootStrapCode()

                                                                Dim rootNode As TreeNode

                                                                TreeViewDirs.Nodes.Clear()
                                                                rootNode = New TreeNode(volName, -1, -1)
                                                                TreeViewDirs.Nodes.Add(rootNode)

                                                                DisplayFileSystem(rootNode, sdf.RootDirectoryEntries(selectedParitionIndex))
                                                            End Sub

        For i As Integer = 0 To sdf.MasterBootRecord.Partitions.Length - 1
            Select Case sdf.MasterBootRecord.Partitions(i).SystemId
                Case StandardDiskFormat.SystemIds.FAT_12, StandardDiskFormat.SystemIds.FAT_16, StandardDiskFormat.SystemIds.FAT_BIGDOS
                    ComboBoxPartitions.Items.Add(sdf.MasterBootRecord.Partitions(i).ToString() +
                                             $" {If(sdf.IsBootable(i), "BOOT", "")} [H:{sdf.Heads(i)} C:{sdf.Cylinders(i)} S:{sdf.Sectors(i)}]")
            End Select
        Next

        If ComboBoxPartitions.Items.Count > 0 Then ComboBoxPartitions.SelectedIndex = 0
    End Sub

    Private Sub DisplayFileSystem(parentNode As TreeNode, entries() As Object)
        parentNode.Nodes.Clear()
        ListViewFileSystem.Items.Clear()

        If entries Is Nothing Then entries = sdf.RootDirectoryEntries(selectedParitionIndex)

        If entries IsNot Nothing Then

            Dim directories = From de In entries
                              Where (de.Attribute And FAT12.EntryAttributes.Directory) = FAT12.EntryAttributes.Directory AndAlso
                                    Convert.ToByte(de.FileNameChars(0)) < &H5E
                              Order By de.FileName
            Dim files = From de In entries
                        Where (de.Attribute And FAT12.EntryAttributes.Directory) <> FAT12.EntryAttributes.Directory AndAlso
                              (de.Attribute And FAT12.EntryAttributes.VolumeName) <> FAT12.EntryAttributes.VolumeName AndAlso
                              Convert.ToByte(de.FileNameChars(0)) < &H5E
                        Order By GetTypeDescription(de.FileExtension)

            Dim node As TreeNode = Nothing
            For Each d In directories
                If d.FileName <> "." AndAlso d.FileName <> ".." Then
                    node = FindNode(d, parentNode)

                    If node Is Nothing Then
                        node = New TreeNode(d.FileName, 0, 0) With {.Tag = d}
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
                        .Add(CDbl(Math.Ceiling((f.FileSize / 1024))).ToString("N0") + " KB")
                        .Add(GetTypeDescription($".{f.FileExtension}"))
                        .Add($"{f.WriteDateTime.ToShortDateString()} {f.WriteDateTime.ToLongTimeString()}")
                    End With
                    .Tag = {node, f}
                End With
            Next
        End If

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

    Private Function FindNode(d As Object, parentNode As TreeNode) As TreeNode
        For Each n As TreeNode In parentNode.Nodes
            If n.Tag IsNot Nothing AndAlso n.Tag.Equals(d) Then
                Return n
            ElseIf n.Nodes.Count > 0 Then
                n = FindNode(d, n)
                If n?.Tag IsNot Nothing AndAlso n.Tag.Equals(d) Then Return n
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

        Dim entry As Object = node.Tag
        DisplayFileSystem(node, sdf.GetDirectoryEntries(selectedParitionIndex, If(entry?.StartingClusterValue = 0, -1, entry.StartingClusterValue)))
    End Sub

    Private Sub DecodeBootStrapCode()
        Dim emu As New X8086(True)
        Dim ins As X8086.Instruction
        Dim address As String
        Dim bsc() As Byte = sdf.BootSector(selectedParitionIndex).BootStrapCode
        Array.Copy(bsc, 0, emu.Memory, 0, bsc.Length)

        ListViewCode.Items.Clear()
        If Not sdf.MasterBootRecord.IsBootable Then Exit Sub

        For i As Integer = 0 To bsc.Length - 1
            address = X8086.SegmentOffsetToAbsolute(0, i).ToString("X")
            ins = emu.Decode(0, i)

            With ListViewCode.Items.Add(address, ins.CS.ToString("X4") + ":" + ins.IP.ToString("X4"), 0)
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

        AutoSizeLastColumn(ListViewCode)
    End Sub

    Private Function GetBytesString(b() As Byte) As String
        Dim r As String = ""
        If b IsNot Nothing Then
            For i As Integer = 0 To b.Length - 1
                r += b(i).ToString("X") + " "
            Next
        End If
        Return r.Trim()
    End Function

    Private Sub InitListView(lv As ListView)
        ListViewHelper.EnableDoubleBuffer(lv)

        Dim item As ListViewItem = Nothing
        item = lv.Items.Add("FFFF:FFFF".Replace("F", " "))
        With item
            .SubItems.Add("FF FF FF FF FF FF".Replace("F", " "))
            .SubItems.Add("FFFFFF".Replace("F", " "))
            .SubItems.Add("FFFFFFFFFFFFFFFFFFFF".Replace("F", " "))
        End With

        lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)
        item.Remove()
    End Sub

    Private Sub AutoSizeLastColumn(lv As ListView)
        Dim w As Integer = lv.ClientSize.Width
        Select Case lv.BorderStyle
            Case BorderStyle.Fixed3D : w -= 4
            Case BorderStyle.FixedSingle : w -= (2 + 16)
        End Select
        For i As Integer = 0 To lv.Columns.Count - 1
            w -= lv.Columns(i).Width
            If w < 1 Then Exit For
        Next

        lv.Columns(lv.Columns.Count - 1).Width += w
    End Sub

    Private Sub ListViewFileSystem_DoubleClick(sender As Object, e As EventArgs) Handles ListViewFileSystem.DoubleClick
        isLeftMouseDown = False
        If ListViewFileSystem.SelectedItems.Count <> 1 Then Exit Sub

        Dim slvi As ListViewItem = ListViewFileSystem.SelectedItems(0)
        If slvi.Tag IsNot Nothing Then ' It's a folder or a file
            Dim objs() As Object = CType(slvi.Tag, Object())
            Dim node As TreeNode = CType(objs(0), TreeNode)
            Dim entry As Object = objs(1)
            If (entry.Attribute And FAT12.EntryAttributes.Directory) = FAT12.EntryAttributes.Directory Then ' It's a directory
                DisplayFileSystem(node, sdf.GetDirectoryEntries(0, If(entry?.StartingClusterValue = 0, -1, entry.StartingClusterValue)))
            Else ' It's a file
                Dim b() As Byte = sdf.ReadFile(selectedParitionIndex, entry)
                Dim targetFileName As String = IO.Path.Combine(IO.Path.GetTempPath(), entry.FullFileName)
                IO.File.WriteAllBytes(targetFileName, b)
                Try
                    Process.Start(targetFileName).WaitForExit()
                    IO.File.Delete(targetFileName)
                Catch ex As Exception
                    MsgBox(ex.Message, MsgBoxStyle.Exclamation)
                End Try
            End If
        End If
    End Sub

    Private Sub SaveDirectory(tmpDirectory As String, entry As Object)
        For Each subEntry As Object In sdf.GetDirectoryEntries(0, entry.StartingClusterValue)
            If subEntry.FileName.StartsWith(".") Then Continue For
            If (subEntry.Attribute And FAT12.EntryAttributes.Directory) = FAT12.EntryAttributes.Directory Then ' It's a directory
                SaveDirectory(tmpDirectory, subEntry)
            Else
                SaveFile(tmpDirectory, subEntry)
            End If
        Next
    End Sub

    Private Function SaveFile(tmpDirectory As String, entry As Object) As String
        Dim targetFileName As String = IO.Path.Combine(tmpDirectory, entry.FullFileName)
        If IO.File.Exists(targetFileName) Then IO.File.Delete(targetFileName)
        IO.File.WriteAllBytes(targetFileName, sdf.ReadFile(selectedParitionIndex, entry))
        Return targetFileName
    End Function

    Private Sub ListViewFileSystem_MouseMove(sender As Object, e As MouseEventArgs) Handles ListViewFileSystem.MouseMove
        If Not isLeftMouseDown OrElse draggedItems.Count > 0 Then Exit Sub
        If Math.Sqrt((mouseDownLocation.X - e.X) ^ 2 + (mouseDownLocation.Y - e.Y) ^ 2) < 6 Then Exit Sub

        Dim tmpDirectory As String = IO.Path.GetTempPath()

        For Each slvi As ListViewItem In ListViewFileSystem.SelectedItems
            If slvi.Tag IsNot Nothing Then ' It's a folder or a file
                Dim entry As Object = CType(slvi.Tag, Object())(1)
                If (entry.Attribute And FAT12.EntryAttributes.Directory) = FAT12.EntryAttributes.Directory Then ' It's a directory
                    Dim subDirectory As String = IO.Path.Combine(tmpDirectory, entry.FileName)
                    If Not IO.Directory.Exists(subDirectory) Then IO.Directory.CreateDirectory(subDirectory)
                    SaveDirectory(subDirectory, entry)
                    draggedItems.Add(subDirectory)
                Else ' It's a file
                    draggedItems.Add(SaveFile(tmpDirectory, entry))
                End If
            End If
        Next

        If draggedItems.Count > 0 Then ListViewFileSystem.DoDragDrop(New DataObject(DataFormats.FileDrop, draggedItems.ToArray()), DragDropEffects.Move)
    End Sub

    Private Sub ListViewFileSystem_MouseDown(sender As Object, e As MouseEventArgs) Handles ListViewFileSystem.MouseDown
        isLeftMouseDown = (e.Button = MouseButtons.Left)
        mouseDownLocation = e.Location
    End Sub

    Private Sub ListViewFileSystem_MouseUp(sender As Object, e As MouseEventArgs) Handles ListViewFileSystem.MouseUp
        isLeftMouseDown = Not (e.Button = MouseButtons.Left)
        If Not isLeftMouseDown Then
            'draggedItems.ForEach(Sub(di) IO.File.Delete(di))
            draggedItems.Clear()
        End If
    End Sub

    Private Sub ListViewFileSystem_DragDrop(sender As Object, e As DragEventArgs) Handles ListViewFileSystem.DragDrop
        If e.Effect = DragDropEffects.Copy Then
            Dim node As TreeNode = TreeViewDirs.SelectedNode
            Dim de As Object = node.Tag ' Parent folder
            Dim files() As String = CType(e.Data.GetData("FileDrop"), String())

            If showDropWarning Then
                If MsgBox("This feature is still under heavy development and using it may corrupt your disk images." + Environment.NewLine +
                           "Are you sure you want to use it anyway?", MsgBoxStyle.YesNo Or MsgBoxStyle.Question) = MsgBoxResult.Yes Then
                    showDropWarning = False
                Else
                    Exit Sub
                End If
            End If

            For i = 0 To files.Length - 1
                If Not IO.File.Exists(files(i)) Then
                    MessageBox.Show("Dropping directories is not yet supported", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    Exit Sub
                End If
            Next

            For i = 0 To files.Length - 1
                sdf.WriteFile(selectedParitionIndex, de, New IO.FileInfo(files(i)))
            Next

            DisplayFileSystem(node, sdf.GetDirectoryEntries(selectedParitionIndex, If(de?.StartingClusterValue = 0, -1, de.StartingClusterValue)))
        End If
    End Sub

    Private Sub ListViewFileSystem_DragOver(sender As Object, e As DragEventArgs) Handles ListViewFileSystem.DragOver
        e.Effect = DragDropEffects.None

        If e.Data.GetFormats().Contains("FileDrop") Then
            Dim files() As String = CType(e.Data.GetData("FileDrop"), String())
            e.Effect = DragDropEffects.Copy
        End If
    End Sub

    Private Sub ListViewFileSystem_KeyDown(sender As Object, e As KeyEventArgs) Handles ListViewFileSystem.KeyDown
        If ListViewFileSystem.SelectedItems.Count = 0 Then Exit Sub

        If e.KeyCode = Keys.Delete Then
            If showDeleteWarning Then
                If MsgBox("This feature is still under heavy development and using it may corrupt your disk images." + Environment.NewLine +
                           "Are you sure you want to use it anyway?", MsgBoxStyle.YesNo Or MsgBoxStyle.Question) = MsgBoxResult.Yes Then
                    showDeleteWarning = False
                Else
                    Exit Sub
                End If
            End If
        Else
            Exit Sub
        End If

        Dim de As Object
        Dim itemsToDelete As New List(Of Object)
        For Each item As ListViewItem In ListViewFileSystem.SelectedItems
            de = CType(item.Tag, Object())(1)
            If (de.Attribute And FAT12.EntryAttributes.Directory) = FAT12.EntryAttributes.Directory Then
                MsgBox("Deleting dicretories is not yet supported")
                Exit Sub
            End If
            itemsToDelete.Add(de)
        Next

        Dim node As TreeNode = TreeViewDirs.SelectedNode
        de = node.Tag ' Parent folder
        itemsToDelete.ForEach(Sub(itd) sdf.DeleteFile(selectedParitionIndex, de, itd))

        DisplayFileSystem(node, sdf.GetDirectoryEntries(selectedParitionIndex, If(de?.StartingClusterValue = 0, -1, de.StartingClusterValue)))
    End Sub

    'Private Sub ListViewFileSystem_MouseMove(sender As Object, e As MouseEventArgs) Handles ListViewFileSystem.MouseMove
    '    If isMouseDown Then
    '        Dim filesCount As Integer = ListViewFileSystem.SelectedItems.Count
    '        Dim si(filesCount - 1) As DataObjectEx.SelectedItem

    '        For i As Integer = 0 To ListViewFileSystem.SelectedItems.Count - 1
    '            Dim objs() As Object = CType(ListViewFileSystem.SelectedItems(i).Tag, Object())
    '            Dim entry As FAT12_16.DirectoryEntry = CType(objs(1), FAT12_16.DirectoryEntry)

    '            si(i).FileName = entry.FullFileName
    '            si(i).WriteTime = entry.WriteDateTime
    '            si(i).FileSize = entry.FileSize
    '        Next

    '        Dim dox As New DataObjectEx(si, Function(selItem As DataObjectEx.SelectedItem) As Byte()
    '                                            Dim b() As Byte = Nothing
    '                                            Me.Invoke(New MethodInvoker(Sub()
    '                                                                            For i As Integer = 0 To ListViewFileSystem.SelectedItems.Count - 1
    '                                                                                Dim objs() As Object = CType(ListViewFileSystem.SelectedItems(i).Tag, Object())
    '                                                                                Dim entry As FAT12_16.DirectoryEntry = CType(objs(1), FAT12_16.DirectoryEntry)

    '                                                                                If selItem.FileName = entry.FullFileName AndAlso
    '                                                                                    selItem.WriteTime = entry.WriteDateTime AndAlso
    '                                                                                    selItem.FileSize = entry.FileSize Then
    '                                                                                    b = sdf.ReadFile(selectedParitionIndex, entry)
    '                                                                                End If
    '                                                                            Next
    '                                                                        End Sub))

    '                                            Return b
    '                                        End Function)
    '        dox.SetData(NativeMethods.CFSTR_FILEDESCRIPTORW, Nothing)
    '        dox.SetData(NativeMethods.CFSTR_FILECONTENTS, Nothing)
    '        dox.SetData(NativeMethods.CFSTR_PERFORMEDDROPEFFECT, Nothing)

    '        ListViewFileSystem.DoDragDrop(dox, DragDropEffects.All)
    '        'Clipboard.SetDataObject(dox)
    '        isMouseDown = False
    '    End If
    'End Sub

    'Private Sub ListViewFileSystem_MouseUp(sender As Object, e As MouseEventArgs) Handles ListViewFileSystem.MouseUp
    '    'If Clipboard.ContainsFileDropList() Then SendKeys.Send("^V")
    'End Sub
End Class