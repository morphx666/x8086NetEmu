<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormDiskManager
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormDiskManager))
        Me.DiskImgCtrl2 = New x8086NetEmuWinForms.DiskImgCtrl()
        Me.DiskImgCtrl1 = New x8086NetEmuWinForms.DiskImgCtrl()
        Me.SuspendLayout()
        '
        'DiskImgCtrl2
        '
        Me.DiskImgCtrl2.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DiskImgCtrl2.Emulator = Nothing
        Me.DiskImgCtrl2.HardDiskMode = False
        Me.DiskImgCtrl2.Index = 1
        Me.DiskImgCtrl2.Location = New System.Drawing.Point(12, 62)
        Me.DiskImgCtrl2.Name = "DiskImgCtrl2"
        Me.DiskImgCtrl2.Size = New System.Drawing.Size(434, 44)
        Me.DiskImgCtrl2.TabIndex = 1
        '
        'DiskImgCtrl1
        '
        Me.DiskImgCtrl1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DiskImgCtrl1.Emulator = Nothing
        Me.DiskImgCtrl1.HardDiskMode = False
        Me.DiskImgCtrl1.Index = 0
        Me.DiskImgCtrl1.Location = New System.Drawing.Point(12, 12)
        Me.DiskImgCtrl1.Name = "DiskImgCtrl1"
        Me.DiskImgCtrl1.Size = New System.Drawing.Size(434, 44)
        Me.DiskImgCtrl1.TabIndex = 0
        '
        'FormDiskManager
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(458, 114)
        Me.Controls.Add(Me.DiskImgCtrl2)
        Me.Controls.Add(Me.DiskImgCtrl1)
        Me.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MaximumSize = New System.Drawing.Size(9999, 188)
        Me.MinimizeBox = False
        Me.MinimumSize = New System.Drawing.Size(470, 146)
        Me.Name = "FormDiskManager"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Floppy Manager"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents DiskImgCtrl1 As x8086NetEmuWinForms.DiskImgCtrl
    Friend WithEvents DiskImgCtrl2 As x8086NetEmuWinForms.DiskImgCtrl
End Class
