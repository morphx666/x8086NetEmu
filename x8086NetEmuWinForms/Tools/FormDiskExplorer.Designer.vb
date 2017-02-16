<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormDiskExplorer
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormDiskExplorer))
        Me.ListViewFileSystem = New System.Windows.Forms.ListView()
        Me.ColumnHeaderFileName = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColumnHeaderSize = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColumnHeaderType = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColumnHeaderDateModified = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ImageListIcons = New System.Windows.Forms.ImageList(Me.components)
        Me.TreeViewDirs = New System.Windows.Forms.TreeView()
        Me.LabelImageFile = New System.Windows.Forms.Label()
        Me.ComboBoxPartitions = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.LabelSerialNumber = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.LabelOemId = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.LabelVolumeLabel = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.LabelFileSystem = New System.Windows.Forms.Label()
        Me.ListViewCode = New System.Windows.Forms.ListView()
        Me.chCodeAddress = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.chBytes = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.chMnemonic = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.chParameters = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.Label6 = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'ListViewFileSystem
        '
        Me.ListViewFileSystem.AllowDrop = True
        Me.ListViewFileSystem.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ListViewFileSystem.BackColor = System.Drawing.Color.FromArgb(CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer))
        Me.ListViewFileSystem.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColumnHeaderFileName, Me.ColumnHeaderSize, Me.ColumnHeaderType, Me.ColumnHeaderDateModified})
        Me.ListViewFileSystem.ForeColor = System.Drawing.Color.WhiteSmoke
        Me.ListViewFileSystem.FullRowSelect = True
        Me.ListViewFileSystem.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable
        Me.ListViewFileSystem.HideSelection = False
        Me.ListViewFileSystem.Location = New System.Drawing.Point(282, 178)
        Me.ListViewFileSystem.Name = "ListViewFileSystem"
        Me.ListViewFileSystem.Size = New System.Drawing.Size(572, 351)
        Me.ListViewFileSystem.SmallImageList = Me.ImageListIcons
        Me.ListViewFileSystem.TabIndex = 1
        Me.ListViewFileSystem.UseCompatibleStateImageBehavior = False
        Me.ListViewFileSystem.View = System.Windows.Forms.View.Details
        '
        'ColumnHeaderFileName
        '
        Me.ColumnHeaderFileName.Text = "Name"
        '
        'ColumnHeaderSize
        '
        Me.ColumnHeaderSize.Text = "Size"
        Me.ColumnHeaderSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'ColumnHeaderType
        '
        Me.ColumnHeaderType.Text = "Type"
        '
        'ColumnHeaderDateModified
        '
        Me.ColumnHeaderDateModified.Text = "Date Modified"
        Me.ColumnHeaderDateModified.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'ImageListIcons
        '
        Me.ImageListIcons.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit
        Me.ImageListIcons.ImageSize = New System.Drawing.Size(16, 16)
        Me.ImageListIcons.TransparentColor = System.Drawing.Color.Transparent
        '
        'TreeViewDirs
        '
        Me.TreeViewDirs.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.TreeViewDirs.BackColor = System.Drawing.Color.FromArgb(CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer))
        Me.TreeViewDirs.ForeColor = System.Drawing.Color.WhiteSmoke
        Me.TreeViewDirs.FullRowSelect = True
        Me.TreeViewDirs.HideSelection = False
        Me.TreeViewDirs.ImageIndex = 0
        Me.TreeViewDirs.ImageList = Me.ImageListIcons
        Me.TreeViewDirs.Location = New System.Drawing.Point(12, 178)
        Me.TreeViewDirs.Name = "TreeViewDirs"
        Me.TreeViewDirs.SelectedImageIndex = 0
        Me.TreeViewDirs.Size = New System.Drawing.Size(264, 351)
        Me.TreeViewDirs.TabIndex = 2
        '
        'LabelImageFile
        '
        Me.LabelImageFile.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LabelImageFile.AutoEllipsis = True
        Me.LabelImageFile.ForeColor = System.Drawing.Color.White
        Me.LabelImageFile.Location = New System.Drawing.Point(124, 9)
        Me.LabelImageFile.Name = "LabelImageFile"
        Me.LabelImageFile.Size = New System.Drawing.Size(286, 23)
        Me.LabelImageFile.TabIndex = 3
        Me.LabelImageFile.Text = "Image File:"
        Me.LabelImageFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabelImageFile.UseMnemonic = False
        '
        'ComboBoxPartitions
        '
        Me.ComboBoxPartitions.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ComboBoxPartitions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxPartitions.FormattingEnabled = True
        Me.ComboBoxPartitions.Location = New System.Drawing.Point(340, 9)
        Me.ComboBoxPartitions.Name = "ComboBoxPartitions"
        Me.ComboBoxPartitions.Size = New System.Drawing.Size(514, 23)
        Me.ComboBoxPartitions.TabIndex = 4
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.ForeColor = System.Drawing.Color.Gainsboro
        Me.Label1.Location = New System.Drawing.Point(12, 13)
        Me.Label1.Margin = New System.Windows.Forms.Padding(3)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(64, 15)
        Me.Label1.TabIndex = 5
        Me.Label1.Text = "Image File:"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.ForeColor = System.Drawing.Color.Gainsboro
        Me.Label2.Location = New System.Drawing.Point(12, 97)
        Me.Label2.Margin = New System.Windows.Forms.Padding(3)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(85, 15)
        Me.Label2.TabIndex = 6
        Me.Label2.Text = "Serial Number:"
        '
        'LabelSerialNumber
        '
        Me.LabelSerialNumber.AutoSize = True
        Me.LabelSerialNumber.ForeColor = System.Drawing.Color.White
        Me.LabelSerialNumber.Location = New System.Drawing.Point(121, 97)
        Me.LabelSerialNumber.Name = "LabelSerialNumber"
        Me.LabelSerialNumber.Size = New System.Drawing.Size(55, 15)
        Me.LabelSerialNumber.TabIndex = 6
        Me.LabelSerialNumber.Text = "00000000"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.ForeColor = System.Drawing.Color.Gainsboro
        Me.Label3.Location = New System.Drawing.Point(12, 76)
        Me.Label3.Margin = New System.Windows.Forms.Padding(3)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(49, 15)
        Me.Label3.TabIndex = 6
        Me.Label3.Text = "OEM Id:"
        '
        'LabelOemId
        '
        Me.LabelOemId.AutoSize = True
        Me.LabelOemId.ForeColor = System.Drawing.Color.White
        Me.LabelOemId.Location = New System.Drawing.Point(121, 76)
        Me.LabelOemId.Name = "LabelOemId"
        Me.LabelOemId.Size = New System.Drawing.Size(55, 15)
        Me.LabelOemId.TabIndex = 6
        Me.LabelOemId.Text = "00000000"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.ForeColor = System.Drawing.Color.Gainsboro
        Me.Label4.Location = New System.Drawing.Point(12, 34)
        Me.Label4.Margin = New System.Windows.Forms.Padding(3)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(81, 15)
        Me.Label4.TabIndex = 6
        Me.Label4.Text = "Volume Label:"
        '
        'LabelVolumeLabel
        '
        Me.LabelVolumeLabel.AutoSize = True
        Me.LabelVolumeLabel.ForeColor = System.Drawing.Color.White
        Me.LabelVolumeLabel.Location = New System.Drawing.Point(121, 34)
        Me.LabelVolumeLabel.Name = "LabelVolumeLabel"
        Me.LabelVolumeLabel.Size = New System.Drawing.Size(55, 15)
        Me.LabelVolumeLabel.TabIndex = 6
        Me.LabelVolumeLabel.Text = "00000000"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.ForeColor = System.Drawing.Color.Gainsboro
        Me.Label5.Location = New System.Drawing.Point(12, 55)
        Me.Label5.Margin = New System.Windows.Forms.Padding(3)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(69, 15)
        Me.Label5.TabIndex = 6
        Me.Label5.Text = "File System:"
        '
        'LabelFileSystem
        '
        Me.LabelFileSystem.AutoSize = True
        Me.LabelFileSystem.ForeColor = System.Drawing.Color.White
        Me.LabelFileSystem.Location = New System.Drawing.Point(121, 55)
        Me.LabelFileSystem.Name = "LabelFileSystem"
        Me.LabelFileSystem.Size = New System.Drawing.Size(55, 15)
        Me.LabelFileSystem.TabIndex = 6
        Me.LabelFileSystem.Text = "00000000"
        '
        'ListViewCode
        '
        Me.ListViewCode.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ListViewCode.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.chCodeAddress, Me.chBytes, Me.chMnemonic, Me.chParameters})
        Me.ListViewCode.Font = New System.Drawing.Font("Consolas", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListViewCode.FullRowSelect = True
        Me.ListViewCode.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None
        Me.ListViewCode.HideSelection = False
        Me.ListViewCode.Location = New System.Drawing.Point(282, 38)
        Me.ListViewCode.Name = "ListViewCode"
        Me.ListViewCode.Size = New System.Drawing.Size(572, 134)
        Me.ListViewCode.TabIndex = 7
        Me.ListViewCode.UseCompatibleStateImageBehavior = False
        Me.ListViewCode.View = System.Windows.Forms.View.Details
        '
        'chCodeAddress
        '
        Me.chCodeAddress.Text = "Address"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.ForeColor = System.Drawing.Color.Gainsboro
        Me.Label6.Location = New System.Drawing.Point(279, 13)
        Me.Label6.Margin = New System.Windows.Forms.Padding(3)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(55, 15)
        Me.Label6.TabIndex = 6
        Me.Label6.Text = "Partition:"
        '
        'FormDiskExplorer
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(868, 543)
        Me.Controls.Add(Me.ListViewCode)
        Me.Controls.Add(Me.LabelVolumeLabel)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.LabelFileSystem)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.LabelOemId)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.LabelSerialNumber)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.ComboBoxPartitions)
        Me.Controls.Add(Me.LabelImageFile)
        Me.Controls.Add(Me.TreeViewDirs)
        Me.Controls.Add(Me.ListViewFileSystem)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ForeColor = System.Drawing.Color.WhiteSmoke
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(750, 582)
        Me.Name = "FormDiskExplorer"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Disk Explorer"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents ListViewFileSystem As ListView
    Friend WithEvents ColumnHeaderFileName As ColumnHeader
    Friend WithEvents ColumnHeaderSize As ColumnHeader
    Friend WithEvents ColumnHeaderType As ColumnHeader
    Friend WithEvents ColumnHeaderDateModified As ColumnHeader
    Friend WithEvents TreeViewDirs As TreeView
    Friend WithEvents LabelImageFile As Label
    Friend WithEvents ComboBoxPartitions As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents LabelSerialNumber As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents LabelOemId As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents LabelVolumeLabel As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents LabelFileSystem As Label
    Friend WithEvents ListViewCode As ListView
    Friend WithEvents chCodeAddress As ColumnHeader
    Friend WithEvents chBytes As ColumnHeader
    Friend WithEvents chMnemonic As ColumnHeader
    Friend WithEvents chParameters As ColumnHeader
    Friend WithEvents Label6 As Label
    Friend WithEvents ImageListIcons As ImageList
End Class
