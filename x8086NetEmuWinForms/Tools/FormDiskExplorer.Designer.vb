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
        Me.ListViewFileSystem = New System.Windows.Forms.ListView()
        Me.ColumnHeaderFileName = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColumnHeaderSize = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColumnHeaderType = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColumnHeaderDateModified = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.SuspendLayout()
        '
        'ListViewFileSystem
        '
        Me.ListViewFileSystem.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ListViewFileSystem.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColumnHeaderFileName, Me.ColumnHeaderSize, Me.ColumnHeaderType, Me.ColumnHeaderDateModified})
        Me.ListViewFileSystem.FullRowSelect = True
        Me.ListViewFileSystem.Location = New System.Drawing.Point(14, 14)
        Me.ListViewFileSystem.Name = "ListViewFileSystem"
        Me.ListViewFileSystem.Size = New System.Drawing.Size(822, 573)
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
        'FormDiskExplorer
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(850, 601)
        Me.Controls.Add(Me.ListViewFileSystem)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Name = "FormDiskExplorer"
        Me.Text = "FormDiskExplorer"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents ListViewFileSystem As ListView
    Friend WithEvents ColumnHeaderFileName As ColumnHeader
    Friend WithEvents ColumnHeaderSize As ColumnHeader
    Friend WithEvents ColumnHeaderType As ColumnHeader
    Friend WithEvents ColumnHeaderDateModified As ColumnHeader
End Class
