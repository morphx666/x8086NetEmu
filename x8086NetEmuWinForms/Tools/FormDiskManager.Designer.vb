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
        Me.DiskImgCtrlB = New x8086NetEmuWinForms.DiskImgCtrl()
        Me.DiskImgCtrlA = New x8086NetEmuWinForms.DiskImgCtrl()
        Me.DiskImgCtrlD = New x8086NetEmuWinForms.DiskImgCtrl()
        Me.DiskImgCtrlC = New x8086NetEmuWinForms.DiskImgCtrl()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'DiskImgCtrlB
        '
        Me.DiskImgCtrlB.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DiskImgCtrlB.Emulator = Nothing
        Me.DiskImgCtrlB.HardDiskMode = False
        Me.DiskImgCtrlB.Index = 1
        Me.DiskImgCtrlB.Location = New System.Drawing.Point(12, 62)
        Me.DiskImgCtrlB.Name = "DiskImgCtrlB"
        Me.DiskImgCtrlB.Size = New System.Drawing.Size(434, 44)
        Me.DiskImgCtrlB.TabIndex = 1
        '
        'DiskImgCtrlA
        '
        Me.DiskImgCtrlA.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DiskImgCtrlA.Emulator = Nothing
        Me.DiskImgCtrlA.HardDiskMode = False
        Me.DiskImgCtrlA.Index = 0
        Me.DiskImgCtrlA.Location = New System.Drawing.Point(12, 12)
        Me.DiskImgCtrlA.Name = "DiskImgCtrlA"
        Me.DiskImgCtrlA.Size = New System.Drawing.Size(434, 44)
        Me.DiskImgCtrlA.TabIndex = 0
        '
        'DiskImgCtrlD
        '
        Me.DiskImgCtrlD.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DiskImgCtrlD.Emulator = Nothing
        Me.DiskImgCtrlD.HardDiskMode = True
        Me.DiskImgCtrlD.Index = 129
        Me.DiskImgCtrlD.Location = New System.Drawing.Point(12, 162)
        Me.DiskImgCtrlD.Name = "DiskImgCtrlD"
        Me.DiskImgCtrlD.Size = New System.Drawing.Size(434, 44)
        Me.DiskImgCtrlD.TabIndex = 3
        '
        'DiskImgCtrlC
        '
        Me.DiskImgCtrlC.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DiskImgCtrlC.Emulator = Nothing
        Me.DiskImgCtrlC.HardDiskMode = True
        Me.DiskImgCtrlC.Index = 128
        Me.DiskImgCtrlC.Location = New System.Drawing.Point(12, 112)
        Me.DiskImgCtrlC.Name = "DiskImgCtrlC"
        Me.DiskImgCtrlC.Size = New System.Drawing.Size(434, 44)
        Me.DiskImgCtrlC.TabIndex = 2
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(260, 4)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(27, 13)
        Me.Label1.TabIndex = 4
        Me.Label1.Text = "R/O"
        '
        'FormDiskManager
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(458, 216)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.DiskImgCtrlD)
        Me.Controls.Add(Me.DiskImgCtrlC)
        Me.Controls.Add(Me.DiskImgCtrlB)
        Me.Controls.Add(Me.DiskImgCtrlA)
        Me.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MaximumSize = New System.Drawing.Size(9999, 255)
        Me.MinimizeBox = False
        Me.MinimumSize = New System.Drawing.Size(474, 255)
        Me.Name = "FormDiskManager"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Media Manager"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents DiskImgCtrlA As x8086NetEmuWinForms.DiskImgCtrl
    Friend WithEvents DiskImgCtrlB As x8086NetEmuWinForms.DiskImgCtrl
    Friend WithEvents DiskImgCtrlD As DiskImgCtrl
    Friend WithEvents DiskImgCtrlC As DiskImgCtrl
    Friend WithEvents Label1 As Label
End Class
