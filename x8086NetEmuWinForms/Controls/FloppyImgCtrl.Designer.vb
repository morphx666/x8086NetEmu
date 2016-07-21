<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FloppyImgCtrl
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
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
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TextBoxImageFileName = New System.Windows.Forms.TextBox()
        Me.ButtonLoad = New System.Windows.Forms.Button()
        Me.ButtonEject = New System.Windows.Forms.Button()
        Me.CheckBoxReadOnly = New System.Windows.Forms.CheckBox()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(3, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(38, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Floppy"
        '
        'TextBoxImageFileName
        '
        Me.TextBoxImageFileName.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBoxImageFileName.BackColor = System.Drawing.SystemColors.Window
        Me.TextBoxImageFileName.Location = New System.Drawing.Point(6, 16)
        Me.TextBoxImageFileName.Name = "TextBoxImageFileName"
        Me.TextBoxImageFileName.ReadOnly = True
        Me.TextBoxImageFileName.Size = New System.Drawing.Size(393, 20)
        Me.TextBoxImageFileName.TabIndex = 1
        '
        'ButtonLoad
        '
        Me.ButtonLoad.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonLoad.Location = New System.Drawing.Point(426, 14)
        Me.ButtonLoad.Name = "ButtonLoad"
        Me.ButtonLoad.Size = New System.Drawing.Size(75, 23)
        Me.ButtonLoad.TabIndex = 0
        Me.ButtonLoad.Text = "Load"
        Me.ButtonLoad.UseVisualStyleBackColor = True
        '
        'ButtonEject
        '
        Me.ButtonEject.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonEject.Location = New System.Drawing.Point(507, 14)
        Me.ButtonEject.Name = "ButtonEject"
        Me.ButtonEject.Size = New System.Drawing.Size(75, 23)
        Me.ButtonEject.TabIndex = 2
        Me.ButtonEject.Text = "Eject"
        Me.ButtonEject.UseVisualStyleBackColor = True
        '
        'CheckBoxReadOnly
        '
        Me.CheckBoxReadOnly.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.CheckBoxReadOnly.AutoSize = True
        Me.CheckBoxReadOnly.Enabled = False
        Me.CheckBoxReadOnly.Location = New System.Drawing.Point(405, 19)
        Me.CheckBoxReadOnly.Name = "CheckBoxReadOnly"
        Me.CheckBoxReadOnly.Size = New System.Drawing.Size(15, 14)
        Me.CheckBoxReadOnly.TabIndex = 3
        Me.CheckBoxReadOnly.UseVisualStyleBackColor = True
        '
        'FloppyImgCtrl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.CheckBoxReadOnly)
        Me.Controls.Add(Me.ButtonEject)
        Me.Controls.Add(Me.ButtonLoad)
        Me.Controls.Add(Me.TextBoxImageFileName)
        Me.Controls.Add(Me.Label1)
        Me.Name = "FloppyImgCtrl"
        Me.Size = New System.Drawing.Size(585, 43)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents TextBoxImageFileName As System.Windows.Forms.TextBox
    Friend WithEvents ButtonLoad As System.Windows.Forms.Button
    Friend WithEvents ButtonEject As System.Windows.Forms.Button
    Friend WithEvents CheckBoxReadOnly As System.Windows.Forms.CheckBox

End Class
