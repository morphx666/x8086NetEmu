<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class DiskImgCtrl
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(DiskImgCtrl))
        Me.LabelDriveInfo = New System.Windows.Forms.Label()
        Me.TextBoxImageFileName = New System.Windows.Forms.TextBox()
        Me.CheckBoxReadOnly = New System.Windows.Forms.CheckBox()
        Me.ButtonView = New x8086NetEmuWinForms.ButtonIcon()
        Me.ButtonEject = New x8086NetEmuWinForms.ButtonIcon()
        Me.ButtonLoad = New x8086NetEmuWinForms.ButtonIcon()
        Me.SuspendLayout()
        '
        'LabelDriveInfo
        '
        Me.LabelDriveInfo.AutoSize = True
        Me.LabelDriveInfo.Location = New System.Drawing.Point(-3, 3)
        Me.LabelDriveInfo.Name = "LabelDriveInfo"
        Me.LabelDriveInfo.Size = New System.Drawing.Size(38, 13)
        Me.LabelDriveInfo.TabIndex = 0
        Me.LabelDriveInfo.Text = "Floppy"
        Me.LabelDriveInfo.UseMnemonic = False
        '
        'TextBoxImageFileName
        '
        Me.TextBoxImageFileName.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBoxImageFileName.BackColor = System.Drawing.Color.DimGray
        Me.TextBoxImageFileName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.TextBoxImageFileName.ForeColor = System.Drawing.Color.White
        Me.TextBoxImageFileName.Location = New System.Drawing.Point(-1, 19)
        Me.TextBoxImageFileName.Name = "TextBoxImageFileName"
        Me.TextBoxImageFileName.ReadOnly = True
        Me.TextBoxImageFileName.Size = New System.Drawing.Size(240, 20)
        Me.TextBoxImageFileName.TabIndex = 0
        '
        'CheckBoxReadOnly
        '
        Me.CheckBoxReadOnly.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.CheckBoxReadOnly.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.CheckBoxReadOnly.FlatAppearance.CheckedBackColor = System.Drawing.Color.DeepSkyBlue
        Me.CheckBoxReadOnly.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxReadOnly.Location = New System.Drawing.Point(246, 23)
        Me.CheckBoxReadOnly.Name = "CheckBoxReadOnly"
        Me.CheckBoxReadOnly.Size = New System.Drawing.Size(12, 11)
        Me.CheckBoxReadOnly.TabIndex = 3
        Me.CheckBoxReadOnly.UseVisualStyleBackColor = True
        '
        'ButtonView
        '
        Me.ButtonView.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonView.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonView.FlatAppearance.BorderSize = 0
        Me.ButtonView.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SteelBlue
        Me.ButtonView.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonView.Image = CType(resources.GetObject("ButtonView.Image"), System.Drawing.Image)
        Me.ButtonView.Location = New System.Drawing.Point(357, 16)
        Me.ButtonView.Name = "ButtonView"
        Me.ButtonView.Size = New System.Drawing.Size(23, 23)
        Me.ButtonView.TabIndex = 3
        Me.ButtonView.UseMnemonic = False
        Me.ButtonView.UseVisualStyleBackColor = True
        '
        'ButtonEject
        '
        Me.ButtonEject.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonEject.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonEject.FlatAppearance.BorderSize = 0
        Me.ButtonEject.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SteelBlue
        Me.ButtonEject.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonEject.Image = CType(resources.GetObject("ButtonEject.Image"), System.Drawing.Image)
        Me.ButtonEject.Location = New System.Drawing.Point(328, 16)
        Me.ButtonEject.Name = "ButtonEject"
        Me.ButtonEject.Size = New System.Drawing.Size(23, 23)
        Me.ButtonEject.TabIndex = 2
        Me.ButtonEject.UseMnemonic = False
        Me.ButtonEject.UseVisualStyleBackColor = True
        '
        'ButtonLoad
        '
        Me.ButtonLoad.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonLoad.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonLoad.FlatAppearance.BorderSize = 0
        Me.ButtonLoad.FlatAppearance.MouseOverBackColor = System.Drawing.Color.SteelBlue
        Me.ButtonLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonLoad.Image = CType(resources.GetObject("ButtonLoad.Image"), System.Drawing.Image)
        Me.ButtonLoad.Location = New System.Drawing.Point(299, 16)
        Me.ButtonLoad.Name = "ButtonLoad"
        Me.ButtonLoad.Size = New System.Drawing.Size(23, 23)
        Me.ButtonLoad.TabIndex = 1
        Me.ButtonLoad.UseMnemonic = False
        Me.ButtonLoad.UseVisualStyleBackColor = True
        '
        'DiskImgCtrl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.Controls.Add(Me.CheckBoxReadOnly)
        Me.Controls.Add(Me.ButtonView)
        Me.Controls.Add(Me.ButtonEject)
        Me.Controls.Add(Me.ButtonLoad)
        Me.Controls.Add(Me.TextBoxImageFileName)
        Me.Controls.Add(Me.LabelDriveInfo)
        Me.ForeColor = System.Drawing.Color.WhiteSmoke
        Me.Name = "DiskImgCtrl"
        Me.Size = New System.Drawing.Size(383, 47)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents LabelDriveInfo As System.Windows.Forms.Label
    Friend WithEvents TextBoxImageFileName As System.Windows.Forms.TextBox
    Friend WithEvents ButtonLoad As ButtonIcon
    Friend WithEvents ButtonEject As ButtonIcon
    Friend WithEvents CheckBoxReadOnly As System.Windows.Forms.CheckBox
    Friend WithEvents ButtonView As ButtonIcon
End Class
