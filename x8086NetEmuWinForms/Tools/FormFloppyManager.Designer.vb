<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormFloppyManager
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
        Me.FloppyImgCtrl2 = New x8086NetEmuWinForms.FloppyImgCtrl()
        Me.FloppyImgCtrl1 = New x8086NetEmuWinForms.FloppyImgCtrl()
        Me.SuspendLayout()
        '
        'FloppyImgCtrl2
        '
        Me.FloppyImgCtrl2.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.FloppyImgCtrl2.Emulator = Nothing
        Me.FloppyImgCtrl2.Index = 1
        Me.FloppyImgCtrl2.Location = New System.Drawing.Point(12, 62)
        Me.FloppyImgCtrl2.Name = "FloppyImgCtrl2"
        Me.FloppyImgCtrl2.Size = New System.Drawing.Size(434, 44)
        Me.FloppyImgCtrl2.TabIndex = 1
        '
        'FloppyImgCtrl1
        '
        Me.FloppyImgCtrl1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.FloppyImgCtrl1.Emulator = Nothing
        Me.FloppyImgCtrl1.Index = 0
        Me.FloppyImgCtrl1.Location = New System.Drawing.Point(12, 12)
        Me.FloppyImgCtrl1.Name = "FloppyImgCtrl1"
        Me.FloppyImgCtrl1.Size = New System.Drawing.Size(434, 44)
        Me.FloppyImgCtrl1.TabIndex = 0
        '
        'FormFloppyManager
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(458, 114)
        Me.Controls.Add(Me.FloppyImgCtrl2)
        Me.Controls.Add(Me.FloppyImgCtrl1)
        Me.MaximizeBox = False
        Me.MaximumSize = New System.Drawing.Size(9999, 188)
        Me.MinimizeBox = False
        Me.MinimumSize = New System.Drawing.Size(470, 146)
        Me.Name = "FormFloppyManager"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Floppy Manager"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents FloppyImgCtrl1 As x8086NetEmuWinForms.FloppyImgCtrl
    Friend WithEvents FloppyImgCtrl2 As x8086NetEmuWinForms.FloppyImgCtrl
End Class
