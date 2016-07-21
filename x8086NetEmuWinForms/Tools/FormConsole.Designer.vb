<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormConsole
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormConsole))
        Me.RichTextBoxConsole = New System.Windows.Forms.RichTextBox()
        Me.ContextMenuStripMain = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.CopyToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem1 = New System.Windows.Forms.ToolStripSeparator()
        Me.ClearToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ContextMenuStripMain.SuspendLayout()
        Me.SuspendLayout()
        '
        'RichTextBoxConsole
        '
        Me.RichTextBoxConsole.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.RichTextBoxConsole.BackColor = System.Drawing.Color.Black
        Me.RichTextBoxConsole.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.RichTextBoxConsole.ContextMenuStrip = Me.ContextMenuStripMain
        Me.RichTextBoxConsole.DetectUrls = False
        Me.RichTextBoxConsole.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RichTextBoxConsole.ForeColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(192, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.RichTextBoxConsole.HideSelection = False
        Me.RichTextBoxConsole.Location = New System.Drawing.Point(9, 9)
        Me.RichTextBoxConsole.Margin = New System.Windows.Forms.Padding(0)
        Me.RichTextBoxConsole.Name = "RichTextBoxConsole"
        Me.RichTextBoxConsole.ReadOnly = True
        Me.RichTextBoxConsole.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical
        Me.RichTextBoxConsole.ShortcutsEnabled = False
        Me.RichTextBoxConsole.Size = New System.Drawing.Size(729, 515)
        Me.RichTextBoxConsole.TabIndex = 0
        Me.RichTextBoxConsole.Text = "x8086NetEmu Console"
        Me.RichTextBoxConsole.WordWrap = False
        '
        'ContextMenuStripMain
        '
        Me.ContextMenuStripMain.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CopyToolStripMenuItem, Me.ToolStripMenuItem1, Me.ClearToolStripMenuItem})
        Me.ContextMenuStripMain.Name = "ContextMenuStripMain"
        Me.ContextMenuStripMain.Size = New System.Drawing.Size(103, 54)
        '
        'CopyToolStripMenuItem
        '
        Me.CopyToolStripMenuItem.Name = "CopyToolStripMenuItem"
        Me.CopyToolStripMenuItem.Size = New System.Drawing.Size(102, 22)
        Me.CopyToolStripMenuItem.Text = "Copy"
        '
        'ToolStripMenuItem1
        '
        Me.ToolStripMenuItem1.Name = "ToolStripMenuItem1"
        Me.ToolStripMenuItem1.Size = New System.Drawing.Size(99, 6)
        '
        'ClearToolStripMenuItem
        '
        Me.ClearToolStripMenuItem.Name = "ClearToolStripMenuItem"
        Me.ClearToolStripMenuItem.Size = New System.Drawing.Size(102, 22)
        Me.ClearToolStripMenuItem.Text = "Clear"
        '
        'FormConsole
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(747, 533)
        Me.Controls.Add(Me.RichTextBoxConsole)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "FormConsole"
        Me.Text = "x8086NetEmu Console"
        Me.ContextMenuStripMain.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents RichTextBoxConsole As System.Windows.Forms.RichTextBox
    Friend WithEvents ContextMenuStripMain As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents CopyToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem1 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents ClearToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
End Class
