<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormDebugger
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormDebugger))
        Me.GroupBoxFlags = New System.Windows.Forms.GroupBox()
        Me.CheckBoxTF = New System.Windows.Forms.CheckBox()
        Me.CheckBoxDF = New System.Windows.Forms.CheckBox()
        Me.CheckBoxIF = New System.Windows.Forms.CheckBox()
        Me.CheckBoxAF = New System.Windows.Forms.CheckBox()
        Me.CheckBoxPF = New System.Windows.Forms.CheckBox()
        Me.CheckBoxOF = New System.Windows.Forms.CheckBox()
        Me.CheckBoxSF = New System.Windows.Forms.CheckBox()
        Me.CheckBoxZF = New System.Windows.Forms.CheckBox()
        Me.CheckBoxCF = New System.Windows.Forms.CheckBox()
        Me.GroupBoxRegisters = New System.Windows.Forms.GroupBox()
        Me.ButtonForward = New System.Windows.Forms.Button()
        Me.ButtonBack = New System.Windows.Forms.Button()
        Me.ButtonDecIP = New System.Windows.Forms.Button()
        Me.TextBoxSI = New System.Windows.Forms.TextBox()
        Me.TextBoxDL = New System.Windows.Forms.TextBox()
        Me.TextBoxES = New System.Windows.Forms.TextBox()
        Me.TextBoxBP = New System.Windows.Forms.TextBox()
        Me.TextBoxCL = New System.Windows.Forms.TextBox()
        Me.TextBoxDH = New System.Windows.Forms.TextBox()
        Me.TextBoxDI = New System.Windows.Forms.TextBox()
        Me.TextBoxDS = New System.Windows.Forms.TextBox()
        Me.TextBoxSP = New System.Windows.Forms.TextBox()
        Me.TextBoxSS = New System.Windows.Forms.TextBox()
        Me.TextBoxCH = New System.Windows.Forms.TextBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.TextBoxBL = New System.Windows.Forms.TextBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.TextBoxBH = New System.Windows.Forms.TextBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.TextBoxIP = New System.Windows.Forms.TextBox()
        Me.TextBoxCS = New System.Windows.Forms.TextBox()
        Me.TextBoxAL = New System.Windows.Forms.TextBox()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.TextBoxAH = New System.Windows.Forms.TextBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.GroupBoxStack = New System.Windows.Forms.GroupBox()
        Me.ListViewStack = New System.Windows.Forms.ListView()
        Me.ColumnHeaderAddress = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColumnHeaderValue = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ButtonStep = New System.Windows.Forms.Button()
        Me.ListViewCode = New System.Windows.Forms.ListView()
        Me.ColumnHeaderCodeAddress = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColumnHeaderBytes = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColumnHeaderMnemonic = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColumnHeaderParameters = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ButtonRefresh = New System.Windows.Forms.Button()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.TextBoxBreakIP = New System.Windows.Forms.TextBox()
        Me.TextBoxMem = New System.Windows.Forms.TextBox()
        Me.TextBoxMemSeg = New System.Windows.Forms.TextBox()
        Me.TextBoxMemOff = New System.Windows.Forms.TextBox()
        Me.ButtonRun = New System.Windows.Forms.Button()
        Me.ButtonReboot = New System.Windows.Forms.Button()
        Me.TextBoxBreakCS = New System.Windows.Forms.TextBox()
        Me.ToolTipValueInfo = New System.Windows.Forms.ToolTip(Me.components)
        Me.CheckBoxTextVideoMemory = New System.Windows.Forms.CheckBox()
        Me.CheckBoxBytesOrChars = New System.Windows.Forms.CheckBox()
        Me.TextBoxSearch = New System.Windows.Forms.TextBox()
        Me.ButtonSearch = New System.Windows.Forms.Button()
        Me.ButtonMemForward = New System.Windows.Forms.Button()
        Me.ButtonMemBack = New System.Windows.Forms.Button()
        Me.GroupBoxFlags.SuspendLayout()
        Me.GroupBoxRegisters.SuspendLayout()
        Me.GroupBoxStack.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBoxFlags
        '
        Me.GroupBoxFlags.Controls.Add(Me.CheckBoxTF)
        Me.GroupBoxFlags.Controls.Add(Me.CheckBoxDF)
        Me.GroupBoxFlags.Controls.Add(Me.CheckBoxIF)
        Me.GroupBoxFlags.Controls.Add(Me.CheckBoxAF)
        Me.GroupBoxFlags.Controls.Add(Me.CheckBoxPF)
        Me.GroupBoxFlags.Controls.Add(Me.CheckBoxOF)
        Me.GroupBoxFlags.Controls.Add(Me.CheckBoxSF)
        Me.GroupBoxFlags.Controls.Add(Me.CheckBoxZF)
        Me.GroupBoxFlags.Controls.Add(Me.CheckBoxCF)
        Me.GroupBoxFlags.ForeColor = System.Drawing.Color.WhiteSmoke
        Me.GroupBoxFlags.Location = New System.Drawing.Point(12, 12)
        Me.GroupBoxFlags.Name = "GroupBoxFlags"
        Me.GroupBoxFlags.Size = New System.Drawing.Size(103, 309)
        Me.GroupBoxFlags.TabIndex = 0
        Me.GroupBoxFlags.TabStop = False
        Me.GroupBoxFlags.Text = "Flags"
        '
        'CheckBoxTF
        '
        Me.CheckBoxTF.Appearance = System.Windows.Forms.Appearance.Button
        Me.CheckBoxTF.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.CheckBoxTF.FlatAppearance.CheckedBackColor = System.Drawing.Color.DarkSlateBlue
        Me.CheckBoxTF.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxTF.Location = New System.Drawing.Point(6, 259)
        Me.CheckBoxTF.Name = "CheckBoxTF"
        Me.CheckBoxTF.Size = New System.Drawing.Size(91, 24)
        Me.CheckBoxTF.TabIndex = 8
        Me.CheckBoxTF.Text = "TF: Trap"
        Me.CheckBoxTF.UseVisualStyleBackColor = True
        '
        'CheckBoxDF
        '
        Me.CheckBoxDF.Appearance = System.Windows.Forms.Appearance.Button
        Me.CheckBoxDF.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.CheckBoxDF.FlatAppearance.CheckedBackColor = System.Drawing.Color.DarkSlateBlue
        Me.CheckBoxDF.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxDF.Location = New System.Drawing.Point(6, 229)
        Me.CheckBoxDF.Name = "CheckBoxDF"
        Me.CheckBoxDF.Size = New System.Drawing.Size(91, 24)
        Me.CheckBoxDF.TabIndex = 7
        Me.CheckBoxDF.Text = "DF: Direction"
        Me.CheckBoxDF.UseVisualStyleBackColor = True
        '
        'CheckBoxIF
        '
        Me.CheckBoxIF.Appearance = System.Windows.Forms.Appearance.Button
        Me.CheckBoxIF.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.CheckBoxIF.FlatAppearance.CheckedBackColor = System.Drawing.Color.DarkSlateBlue
        Me.CheckBoxIF.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxIF.Location = New System.Drawing.Point(6, 199)
        Me.CheckBoxIF.Name = "CheckBoxIF"
        Me.CheckBoxIF.Size = New System.Drawing.Size(91, 24)
        Me.CheckBoxIF.TabIndex = 6
        Me.CheckBoxIF.Text = "IF: Interrupt"
        Me.CheckBoxIF.UseVisualStyleBackColor = True
        '
        'CheckBoxAF
        '
        Me.CheckBoxAF.Appearance = System.Windows.Forms.Appearance.Button
        Me.CheckBoxAF.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.CheckBoxAF.FlatAppearance.CheckedBackColor = System.Drawing.Color.DarkSlateBlue
        Me.CheckBoxAF.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxAF.Location = New System.Drawing.Point(6, 169)
        Me.CheckBoxAF.Name = "CheckBoxAF"
        Me.CheckBoxAF.Size = New System.Drawing.Size(91, 24)
        Me.CheckBoxAF.TabIndex = 5
        Me.CheckBoxAF.Text = "AF: Aux"
        Me.CheckBoxAF.UseVisualStyleBackColor = True
        '
        'CheckBoxPF
        '
        Me.CheckBoxPF.Appearance = System.Windows.Forms.Appearance.Button
        Me.CheckBoxPF.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.CheckBoxPF.FlatAppearance.CheckedBackColor = System.Drawing.Color.DarkSlateBlue
        Me.CheckBoxPF.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxPF.Location = New System.Drawing.Point(6, 139)
        Me.CheckBoxPF.Name = "CheckBoxPF"
        Me.CheckBoxPF.Size = New System.Drawing.Size(91, 24)
        Me.CheckBoxPF.TabIndex = 4
        Me.CheckBoxPF.Text = "PF: Parity"
        Me.CheckBoxPF.UseVisualStyleBackColor = True
        '
        'CheckBoxOF
        '
        Me.CheckBoxOF.Appearance = System.Windows.Forms.Appearance.Button
        Me.CheckBoxOF.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.CheckBoxOF.FlatAppearance.CheckedBackColor = System.Drawing.Color.DarkSlateBlue
        Me.CheckBoxOF.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxOF.Location = New System.Drawing.Point(6, 109)
        Me.CheckBoxOF.Name = "CheckBoxOF"
        Me.CheckBoxOF.Size = New System.Drawing.Size(91, 24)
        Me.CheckBoxOF.TabIndex = 3
        Me.CheckBoxOF.Text = "OF: Overflow"
        Me.CheckBoxOF.UseVisualStyleBackColor = True
        '
        'CheckBoxSF
        '
        Me.CheckBoxSF.Appearance = System.Windows.Forms.Appearance.Button
        Me.CheckBoxSF.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.CheckBoxSF.FlatAppearance.CheckedBackColor = System.Drawing.Color.DarkSlateBlue
        Me.CheckBoxSF.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxSF.Location = New System.Drawing.Point(6, 79)
        Me.CheckBoxSF.Name = "CheckBoxSF"
        Me.CheckBoxSF.Size = New System.Drawing.Size(91, 24)
        Me.CheckBoxSF.TabIndex = 2
        Me.CheckBoxSF.Text = "SF: Sign"
        Me.CheckBoxSF.UseVisualStyleBackColor = True
        '
        'CheckBoxZF
        '
        Me.CheckBoxZF.Appearance = System.Windows.Forms.Appearance.Button
        Me.CheckBoxZF.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.CheckBoxZF.FlatAppearance.CheckedBackColor = System.Drawing.Color.DarkSlateBlue
        Me.CheckBoxZF.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxZF.Location = New System.Drawing.Point(6, 49)
        Me.CheckBoxZF.Name = "CheckBoxZF"
        Me.CheckBoxZF.Size = New System.Drawing.Size(91, 24)
        Me.CheckBoxZF.TabIndex = 1
        Me.CheckBoxZF.Text = "ZF: Zero"
        Me.CheckBoxZF.UseVisualStyleBackColor = True
        '
        'CheckBoxCF
        '
        Me.CheckBoxCF.Appearance = System.Windows.Forms.Appearance.Button
        Me.CheckBoxCF.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.CheckBoxCF.FlatAppearance.CheckedBackColor = System.Drawing.Color.DarkSlateBlue
        Me.CheckBoxCF.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxCF.Location = New System.Drawing.Point(6, 19)
        Me.CheckBoxCF.Name = "CheckBoxCF"
        Me.CheckBoxCF.Size = New System.Drawing.Size(91, 24)
        Me.CheckBoxCF.TabIndex = 0
        Me.CheckBoxCF.Text = "CF: Carry"
        Me.CheckBoxCF.UseVisualStyleBackColor = True
        '
        'GroupBoxRegisters
        '
        Me.GroupBoxRegisters.Controls.Add(Me.ButtonForward)
        Me.GroupBoxRegisters.Controls.Add(Me.ButtonBack)
        Me.GroupBoxRegisters.Controls.Add(Me.ButtonDecIP)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxSI)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxDL)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxES)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxBP)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxCL)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxDH)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxDI)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxDS)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxSP)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxSS)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxCH)
        Me.GroupBoxRegisters.Controls.Add(Me.Label8)
        Me.GroupBoxRegisters.Controls.Add(Me.Label4)
        Me.GroupBoxRegisters.Controls.Add(Me.Label13)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxBL)
        Me.GroupBoxRegisters.Controls.Add(Me.Label7)
        Me.GroupBoxRegisters.Controls.Add(Me.Label12)
        Me.GroupBoxRegisters.Controls.Add(Me.Label3)
        Me.GroupBoxRegisters.Controls.Add(Me.Label10)
        Me.GroupBoxRegisters.Controls.Add(Me.Label11)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxBH)
        Me.GroupBoxRegisters.Controls.Add(Me.Label6)
        Me.GroupBoxRegisters.Controls.Add(Me.Label2)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxIP)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxCS)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxAL)
        Me.GroupBoxRegisters.Controls.Add(Me.Label9)
        Me.GroupBoxRegisters.Controls.Add(Me.TextBoxAH)
        Me.GroupBoxRegisters.Controls.Add(Me.Label5)
        Me.GroupBoxRegisters.Controls.Add(Me.Label1)
        Me.GroupBoxRegisters.ForeColor = System.Drawing.Color.WhiteSmoke
        Me.GroupBoxRegisters.Location = New System.Drawing.Point(121, 12)
        Me.GroupBoxRegisters.Name = "GroupBoxRegisters"
        Me.GroupBoxRegisters.Size = New System.Drawing.Size(218, 309)
        Me.GroupBoxRegisters.TabIndex = 1
        Me.GroupBoxRegisters.TabStop = False
        Me.GroupBoxRegisters.Text = "Registers"
        '
        'ButtonForward
        '
        Me.ButtonForward.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonForward.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonForward.Location = New System.Drawing.Point(181, 88)
        Me.ButtonForward.Name = "ButtonForward"
        Me.ButtonForward.Size = New System.Drawing.Size(25, 23)
        Me.ButtonForward.TabIndex = 13
        Me.ButtonForward.Text = ">"
        Me.ButtonForward.UseVisualStyleBackColor = True
        '
        'ButtonBack
        '
        Me.ButtonBack.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonBack.Location = New System.Drawing.Point(137, 89)
        Me.ButtonBack.Name = "ButtonBack"
        Me.ButtonBack.Size = New System.Drawing.Size(25, 23)
        Me.ButtonBack.TabIndex = 12
        Me.ButtonBack.Text = "<"
        Me.ButtonBack.UseVisualStyleBackColor = True
        '
        'ButtonDecIP
        '
        Me.ButtonDecIP.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonDecIP.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonDecIP.Location = New System.Drawing.Point(137, 117)
        Me.ButtonDecIP.Name = "ButtonDecIP"
        Me.ButtonDecIP.Size = New System.Drawing.Size(69, 23)
        Me.ButtonDecIP.TabIndex = 14
        Me.ButtonDecIP.Text = "<<"
        Me.ButtonDecIP.UseVisualStyleBackColor = True
        '
        'TextBoxSI
        '
        Me.TextBoxSI.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxSI.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxSI.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxSI.Location = New System.Drawing.Point(136, 210)
        Me.TextBoxSI.Name = "TextBoxSI"
        Me.TextBoxSI.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxSI.TabIndex = 17
        Me.TextBoxSI.Text = "0000"
        Me.TextBoxSI.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxDL
        '
        Me.TextBoxDL.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxDL.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxDL.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxDL.Location = New System.Drawing.Point(68, 114)
        Me.TextBoxDL.Name = "TextBoxDL"
        Me.TextBoxDL.Size = New System.Drawing.Size(35, 26)
        Me.TextBoxDL.TabIndex = 7
        Me.TextBoxDL.Text = "00"
        Me.TextBoxDL.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxES
        '
        Me.TextBoxES.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxES.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxES.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxES.Location = New System.Drawing.Point(33, 242)
        Me.TextBoxES.Name = "TextBoxES"
        Me.TextBoxES.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxES.TabIndex = 11
        Me.TextBoxES.Text = "0000"
        Me.TextBoxES.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxBP
        '
        Me.TextBoxBP.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxBP.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxBP.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxBP.Location = New System.Drawing.Point(136, 274)
        Me.TextBoxBP.Name = "TextBoxBP"
        Me.TextBoxBP.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxBP.TabIndex = 19
        Me.TextBoxBP.Text = "0000"
        Me.TextBoxBP.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxCL
        '
        Me.TextBoxCL.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxCL.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxCL.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxCL.Location = New System.Drawing.Point(68, 82)
        Me.TextBoxCL.Name = "TextBoxCL"
        Me.TextBoxCL.Size = New System.Drawing.Size(35, 26)
        Me.TextBoxCL.TabIndex = 5
        Me.TextBoxCL.Text = "00"
        Me.TextBoxCL.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxDH
        '
        Me.TextBoxDH.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxDH.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxDH.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxDH.Location = New System.Drawing.Point(33, 114)
        Me.TextBoxDH.Name = "TextBoxDH"
        Me.TextBoxDH.Size = New System.Drawing.Size(35, 26)
        Me.TextBoxDH.TabIndex = 6
        Me.TextBoxDH.Text = "00"
        Me.TextBoxDH.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxDI
        '
        Me.TextBoxDI.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxDI.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxDI.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxDI.Location = New System.Drawing.Point(136, 242)
        Me.TextBoxDI.Name = "TextBoxDI"
        Me.TextBoxDI.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxDI.TabIndex = 18
        Me.TextBoxDI.Text = "0000"
        Me.TextBoxDI.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxDS
        '
        Me.TextBoxDS.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxDS.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxDS.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxDS.Location = New System.Drawing.Point(33, 210)
        Me.TextBoxDS.Name = "TextBoxDS"
        Me.TextBoxDS.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxDS.TabIndex = 10
        Me.TextBoxDS.Text = "0000"
        Me.TextBoxDS.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxSP
        '
        Me.TextBoxSP.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxSP.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxSP.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxSP.Location = New System.Drawing.Point(136, 178)
        Me.TextBoxSP.Name = "TextBoxSP"
        Me.TextBoxSP.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxSP.TabIndex = 16
        Me.TextBoxSP.Text = "0000"
        Me.TextBoxSP.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxSS
        '
        Me.TextBoxSS.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxSS.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxSS.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxSS.Location = New System.Drawing.Point(33, 178)
        Me.TextBoxSS.Name = "TextBoxSS"
        Me.TextBoxSS.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxSS.TabIndex = 9
        Me.TextBoxSS.Text = "0000"
        Me.TextBoxSS.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxCH
        '
        Me.TextBoxCH.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxCH.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxCH.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxCH.Location = New System.Drawing.Point(33, 82)
        Me.TextBoxCH.Name = "TextBoxCH"
        Me.TextBoxCH.Size = New System.Drawing.Size(35, 26)
        Me.TextBoxCH.TabIndex = 4
        Me.TextBoxCH.Text = "00"
        Me.TextBoxCH.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(109, 217)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(16, 13)
        Me.Label8.TabIndex = 3
        Me.Label8.Text = "SI"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(6, 121)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(21, 13)
        Me.Label4.TabIndex = 3
        Me.Label4.Text = "DX"
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.Location = New System.Drawing.Point(6, 249)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(19, 13)
        Me.Label13.TabIndex = 3
        Me.Label13.Text = "ES"
        '
        'TextBoxBL
        '
        Me.TextBoxBL.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxBL.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxBL.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxBL.Location = New System.Drawing.Point(68, 50)
        Me.TextBoxBL.Name = "TextBoxBL"
        Me.TextBoxBL.Size = New System.Drawing.Size(35, 26)
        Me.TextBoxBL.TabIndex = 3
        Me.TextBoxBL.Text = "00"
        Me.TextBoxBL.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(107, 281)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(20, 13)
        Me.Label7.TabIndex = 3
        Me.Label7.Text = "BP"
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(109, 249)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(18, 13)
        Me.Label12.TabIndex = 3
        Me.Label12.Text = "DI"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(6, 89)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(20, 13)
        Me.Label3.TabIndex = 3
        Me.Label3.Text = "CX"
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(109, 185)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(19, 13)
        Me.Label10.TabIndex = 3
        Me.Label10.Text = "SP"
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(6, 217)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(21, 13)
        Me.Label11.TabIndex = 3
        Me.Label11.Text = "DS"
        '
        'TextBoxBH
        '
        Me.TextBoxBH.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxBH.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxBH.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxBH.Location = New System.Drawing.Point(33, 50)
        Me.TextBoxBH.Name = "TextBoxBH"
        Me.TextBoxBH.Size = New System.Drawing.Size(35, 26)
        Me.TextBoxBH.TabIndex = 2
        Me.TextBoxBH.Text = "00"
        Me.TextBoxBH.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(6, 185)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(19, 13)
        Me.Label6.TabIndex = 3
        Me.Label6.Text = "SS"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(6, 57)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(20, 13)
        Me.Label2.TabIndex = 3
        Me.Label2.Text = "BX"
        '
        'TextBoxIP
        '
        Me.TextBoxIP.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxIP.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxIP.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxIP.Location = New System.Drawing.Point(136, 146)
        Me.TextBoxIP.Name = "TextBoxIP"
        Me.TextBoxIP.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxIP.TabIndex = 15
        Me.TextBoxIP.Text = "0000"
        Me.TextBoxIP.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxCS
        '
        Me.TextBoxCS.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxCS.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxCS.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxCS.Location = New System.Drawing.Point(33, 146)
        Me.TextBoxCS.Name = "TextBoxCS"
        Me.TextBoxCS.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxCS.TabIndex = 8
        Me.TextBoxCS.Text = "0000"
        Me.TextBoxCS.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxAL
        '
        Me.TextBoxAL.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxAL.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxAL.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxAL.Location = New System.Drawing.Point(68, 18)
        Me.TextBoxAL.Name = "TextBoxAL"
        Me.TextBoxAL.Size = New System.Drawing.Size(35, 26)
        Me.TextBoxAL.TabIndex = 1
        Me.TextBoxAL.Text = "00"
        Me.TextBoxAL.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(109, 153)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(16, 13)
        Me.Label9.TabIndex = 0
        Me.Label9.Text = "IP"
        '
        'TextBoxAH
        '
        Me.TextBoxAH.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxAH.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxAH.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxAH.Location = New System.Drawing.Point(33, 18)
        Me.TextBoxAH.Name = "TextBoxAH"
        Me.TextBoxAH.Size = New System.Drawing.Size(35, 26)
        Me.TextBoxAH.TabIndex = 0
        Me.TextBoxAH.Text = "00"
        Me.TextBoxAH.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(6, 153)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(20, 13)
        Me.Label5.TabIndex = 0
        Me.Label5.Text = "CS"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(6, 25)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(20, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "AX"
        '
        'GroupBoxStack
        '
        Me.GroupBoxStack.Controls.Add(Me.ListViewStack)
        Me.GroupBoxStack.ForeColor = System.Drawing.Color.WhiteSmoke
        Me.GroupBoxStack.Location = New System.Drawing.Point(345, 12)
        Me.GroupBoxStack.Name = "GroupBoxStack"
        Me.GroupBoxStack.Size = New System.Drawing.Size(200, 309)
        Me.GroupBoxStack.TabIndex = 2
        Me.GroupBoxStack.TabStop = False
        Me.GroupBoxStack.Text = "Stack"
        '
        'ListViewStack
        '
        Me.ListViewStack.BackColor = System.Drawing.Color.FromArgb(CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer))
        Me.ListViewStack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.ListViewStack.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColumnHeaderAddress, Me.ColumnHeaderValue})
        Me.ListViewStack.Font = New System.Drawing.Font("Consolas", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListViewStack.ForeColor = System.Drawing.Color.Gainsboro
        Me.ListViewStack.FullRowSelect = True
        Me.ListViewStack.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None
        Me.ListViewStack.Location = New System.Drawing.Point(6, 18)
        Me.ListViewStack.Name = "ListViewStack"
        Me.ListViewStack.Size = New System.Drawing.Size(188, 285)
        Me.ListViewStack.TabIndex = 0
        Me.ListViewStack.UseCompatibleStateImageBehavior = False
        Me.ListViewStack.View = System.Windows.Forms.View.Details
        '
        'ColumnHeaderAddress
        '
        Me.ColumnHeaderAddress.Text = "Address"
        '
        'ColumnHeaderValue
        '
        Me.ColumnHeaderValue.Text = "Value"
        '
        'ButtonStep
        '
        Me.ButtonStep.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonStep.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonStep.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonStep.Location = New System.Drawing.Point(882, 604)
        Me.ButtonStep.Name = "ButtonStep"
        Me.ButtonStep.Size = New System.Drawing.Size(97, 32)
        Me.ButtonStep.TabIndex = 13
        Me.ButtonStep.Text = "Step"
        Me.ButtonStep.UseVisualStyleBackColor = True
        '
        'ListViewCode
        '
        Me.ListViewCode.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ListViewCode.CheckBoxes = True
        Me.ListViewCode.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColumnHeaderCodeAddress, Me.ColumnHeaderBytes, Me.ColumnHeaderMnemonic, Me.ColumnHeaderParameters})
        Me.ListViewCode.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListViewCode.FullRowSelect = True
        Me.ListViewCode.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None
        Me.ListViewCode.HideSelection = False
        Me.ListViewCode.Location = New System.Drawing.Point(551, 18)
        Me.ListViewCode.Name = "ListViewCode"
        Me.ListViewCode.Size = New System.Drawing.Size(531, 578)
        Me.ListViewCode.TabIndex = 15
        Me.ListViewCode.UseCompatibleStateImageBehavior = False
        Me.ListViewCode.View = System.Windows.Forms.View.Details
        '
        'ColumnHeaderCodeAddress
        '
        Me.ColumnHeaderCodeAddress.Text = "Address"
        '
        'ColumnHeaderBytes
        '
        Me.ColumnHeaderBytes.Text = "Bytes"
        '
        'ColumnHeaderMnemonic
        '
        Me.ColumnHeaderMnemonic.Text = "Mnemonic"
        '
        'ColumnHeaderParameters
        '
        Me.ColumnHeaderParameters.Text = "Parameters"
        '
        'ButtonRefresh
        '
        Me.ButtonRefresh.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonRefresh.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonRefresh.Location = New System.Drawing.Point(779, 604)
        Me.ButtonRefresh.Name = "ButtonRefresh"
        Me.ButtonRefresh.Size = New System.Drawing.Size(97, 32)
        Me.ButtonRefresh.TabIndex = 12
        Me.ButtonRefresh.Text = "Refresh"
        Me.ButtonRefresh.UseVisualStyleBackColor = True
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.Location = New System.Drawing.Point(341, 334)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(50, 13)
        Me.Label14.TabIndex = 6
        Me.Label14.Text = "Break At"
        '
        'TextBoxBreakIP
        '
        Me.TextBoxBreakIP.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxBreakIP.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxBreakIP.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxBreakIP.Location = New System.Drawing.Point(475, 327)
        Me.TextBoxBreakIP.Name = "TextBoxBreakIP"
        Me.TextBoxBreakIP.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxBreakIP.TabIndex = 6
        Me.TextBoxBreakIP.Text = "0000"
        Me.TextBoxBreakIP.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxMem
        '
        Me.TextBoxMem.BackColor = System.Drawing.Color.FromArgb(CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer))
        Me.TextBoxMem.Cursor = System.Windows.Forms.Cursors.Arrow
        Me.TextBoxMem.Font = New System.Drawing.Font("Consolas", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxMem.ForeColor = System.Drawing.Color.Gainsboro
        Me.TextBoxMem.Location = New System.Drawing.Point(12, 359)
        Me.TextBoxMem.Multiline = True
        Me.TextBoxMem.Name = "TextBoxMem"
        Me.TextBoxMem.ReadOnly = True
        Me.TextBoxMem.Size = New System.Drawing.Size(533, 237)
        Me.TextBoxMem.TabIndex = 7
        '
        'TextBoxMemSeg
        '
        Me.TextBoxMemSeg.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxMemSeg.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxMemSeg.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxMemSeg.Location = New System.Drawing.Point(12, 327)
        Me.TextBoxMemSeg.Name = "TextBoxMemSeg"
        Me.TextBoxMemSeg.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxMemSeg.TabIndex = 1
        Me.TextBoxMemSeg.Text = "0000"
        Me.TextBoxMemSeg.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'TextBoxMemOff
        '
        Me.TextBoxMemOff.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxMemOff.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxMemOff.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxMemOff.Location = New System.Drawing.Point(88, 327)
        Me.TextBoxMemOff.Name = "TextBoxMemOff"
        Me.TextBoxMemOff.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxMemOff.TabIndex = 2
        Me.TextBoxMemOff.Text = "0000"
        Me.TextBoxMemOff.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'ButtonRun
        '
        Me.ButtonRun.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonRun.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonRun.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonRun.Location = New System.Drawing.Point(985, 604)
        Me.ButtonRun.Name = "ButtonRun"
        Me.ButtonRun.Size = New System.Drawing.Size(97, 32)
        Me.ButtonRun.TabIndex = 14
        Me.ButtonRun.Text = "Run"
        Me.ButtonRun.UseVisualStyleBackColor = True
        '
        'ButtonReboot
        '
        Me.ButtonReboot.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonReboot.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonReboot.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonReboot.Location = New System.Drawing.Point(551, 604)
        Me.ButtonReboot.Name = "ButtonReboot"
        Me.ButtonReboot.Size = New System.Drawing.Size(97, 32)
        Me.ButtonReboot.TabIndex = 10
        Me.ButtonReboot.Text = "Reboot"
        Me.ButtonReboot.UseVisualStyleBackColor = True
        '
        'TextBoxBreakCS
        '
        Me.TextBoxBreakCS.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxBreakCS.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxBreakCS.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxBreakCS.Location = New System.Drawing.Point(399, 327)
        Me.TextBoxBreakCS.Name = "TextBoxBreakCS"
        Me.TextBoxBreakCS.Size = New System.Drawing.Size(70, 26)
        Me.TextBoxBreakCS.TabIndex = 5
        Me.TextBoxBreakCS.Text = "0000"
        Me.TextBoxBreakCS.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'ToolTipValueInfo
        '
        Me.ToolTipValueInfo.AutomaticDelay = 100
        Me.ToolTipValueInfo.AutoPopDelay = 10000
        Me.ToolTipValueInfo.InitialDelay = 100
        Me.ToolTipValueInfo.IsBalloon = True
        Me.ToolTipValueInfo.ReshowDelay = 20
        Me.ToolTipValueInfo.UseAnimation = False
        Me.ToolTipValueInfo.UseFading = False
        '
        'CheckBoxTextVideoMemory
        '
        Me.CheckBoxTextVideoMemory.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.CheckBoxTextVideoMemory.AutoSize = True
        Me.CheckBoxTextVideoMemory.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxTextVideoMemory.Location = New System.Drawing.Point(345, 615)
        Me.CheckBoxTextVideoMemory.Name = "CheckBoxTextVideoMemory"
        Me.CheckBoxTextVideoMemory.Size = New System.Drawing.Size(12, 11)
        Me.CheckBoxTextVideoMemory.TabIndex = 8
        Me.ToolTipValueInfo.SetToolTip(Me.CheckBoxTextVideoMemory, "Enable Text Video Memory compatibility search")
        Me.CheckBoxTextVideoMemory.UseVisualStyleBackColor = True
        '
        'CheckBoxBytesOrChars
        '
        Me.CheckBoxBytesOrChars.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.CheckBoxBytesOrChars.AutoSize = True
        Me.CheckBoxBytesOrChars.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBoxBytesOrChars.Location = New System.Drawing.Point(761, 615)
        Me.CheckBoxBytesOrChars.Name = "CheckBoxBytesOrChars"
        Me.CheckBoxBytesOrChars.Size = New System.Drawing.Size(12, 11)
        Me.CheckBoxBytesOrChars.TabIndex = 11
        Me.ToolTipValueInfo.SetToolTip(Me.CheckBoxBytesOrChars, "Toggle bytes/chars display")
        Me.CheckBoxBytesOrChars.UseVisualStyleBackColor = True
        '
        'TextBoxSearch
        '
        Me.TextBoxSearch.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.TextBoxSearch.BackColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer))
        Me.TextBoxSearch.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxSearch.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.TextBoxSearch.Location = New System.Drawing.Point(12, 607)
        Me.TextBoxSearch.Name = "TextBoxSearch"
        Me.TextBoxSearch.Size = New System.Drawing.Size(327, 26)
        Me.TextBoxSearch.TabIndex = 7
        '
        'ButtonSearch
        '
        Me.ButtonSearch.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonSearch.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonSearch.Location = New System.Drawing.Point(366, 604)
        Me.ButtonSearch.Name = "ButtonSearch"
        Me.ButtonSearch.Size = New System.Drawing.Size(97, 32)
        Me.ButtonSearch.TabIndex = 9
        Me.ButtonSearch.Text = "Search"
        Me.ButtonSearch.UseVisualStyleBackColor = True
        '
        'ButtonMemForward
        '
        Me.ButtonMemForward.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonMemForward.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonMemForward.Location = New System.Drawing.Point(195, 329)
        Me.ButtonMemForward.Name = "ButtonMemForward"
        Me.ButtonMemForward.Size = New System.Drawing.Size(25, 23)
        Me.ButtonMemForward.TabIndex = 4
        Me.ButtonMemForward.Text = ">"
        Me.ButtonMemForward.UseVisualStyleBackColor = True
        '
        'ButtonMemBack
        '
        Me.ButtonMemBack.FlatAppearance.BorderColor = System.Drawing.Color.DimGray
        Me.ButtonMemBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonMemBack.Location = New System.Drawing.Point(164, 329)
        Me.ButtonMemBack.Name = "ButtonMemBack"
        Me.ButtonMemBack.Size = New System.Drawing.Size(25, 23)
        Me.ButtonMemBack.TabIndex = 3
        Me.ButtonMemBack.Text = "<"
        Me.ButtonMemBack.UseVisualStyleBackColor = True
        '
        'FormDebugger
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer), CType(CType(33, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(1094, 648)
        Me.Controls.Add(Me.CheckBoxBytesOrChars)
        Me.Controls.Add(Me.ButtonMemForward)
        Me.Controls.Add(Me.ButtonMemBack)
        Me.Controls.Add(Me.CheckBoxTextVideoMemory)
        Me.Controls.Add(Me.ButtonSearch)
        Me.Controls.Add(Me.TextBoxSearch)
        Me.Controls.Add(Me.ButtonReboot)
        Me.Controls.Add(Me.TextBoxMem)
        Me.Controls.Add(Me.Label14)
        Me.Controls.Add(Me.ListViewCode)
        Me.Controls.Add(Me.ButtonRun)
        Me.Controls.Add(Me.ButtonRefresh)
        Me.Controls.Add(Me.ButtonStep)
        Me.Controls.Add(Me.GroupBoxStack)
        Me.Controls.Add(Me.GroupBoxRegisters)
        Me.Controls.Add(Me.GroupBoxFlags)
        Me.Controls.Add(Me.TextBoxBreakCS)
        Me.Controls.Add(Me.TextBoxBreakIP)
        Me.Controls.Add(Me.TextBoxMemSeg)
        Me.Controls.Add(Me.TextBoxMemOff)
        Me.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ForeColor = System.Drawing.Color.WhiteSmoke
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.KeyPreview = True
        Me.MinimumSize = New System.Drawing.Size(1095, 687)
        Me.Name = "FormDebugger"
        Me.Text = "Debugger"
        Me.GroupBoxFlags.ResumeLayout(False)
        Me.GroupBoxRegisters.ResumeLayout(False)
        Me.GroupBoxRegisters.PerformLayout()
        Me.GroupBoxStack.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents GroupBoxFlags As System.Windows.Forms.GroupBox
    Friend WithEvents CheckBoxOF As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBoxSF As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBoxZF As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBoxCF As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBoxAF As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBoxPF As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBoxDF As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBoxIF As System.Windows.Forms.CheckBox
    Friend WithEvents GroupBoxRegisters As System.Windows.Forms.GroupBox
    Friend WithEvents TextBoxAL As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxAH As System.Windows.Forms.TextBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents TextBoxBL As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxBH As System.Windows.Forms.TextBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents TextBoxDL As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxCL As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxDH As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxCH As System.Windows.Forms.TextBox
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents TextBoxSI As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxBP As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxSS As System.Windows.Forms.TextBox
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents TextBoxCS As System.Windows.Forms.TextBox
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents TextBoxSP As System.Windows.Forms.TextBox
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents TextBoxIP As System.Windows.Forms.TextBox
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents TextBoxES As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxDI As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxDS As System.Windows.Forms.TextBox
    Friend WithEvents Label13 As System.Windows.Forms.Label
    Friend WithEvents Label12 As System.Windows.Forms.Label
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents GroupBoxStack As System.Windows.Forms.GroupBox
    Friend WithEvents ListViewStack As System.Windows.Forms.ListView
    Friend WithEvents ColumnHeaderAddress As System.Windows.Forms.ColumnHeader
    Friend WithEvents ColumnHeaderValue As System.Windows.Forms.ColumnHeader
    Friend WithEvents ButtonStep As System.Windows.Forms.Button
    Friend WithEvents ListViewCode As System.Windows.Forms.ListView
    Friend WithEvents ColumnHeaderCodeAddress As System.Windows.Forms.ColumnHeader
    Friend WithEvents ButtonRefresh As System.Windows.Forms.Button
    Friend WithEvents Label14 As System.Windows.Forms.Label
    Friend WithEvents TextBoxBreakIP As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxMem As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxMemSeg As System.Windows.Forms.TextBox
    Friend WithEvents TextBoxMemOff As System.Windows.Forms.TextBox
    Friend WithEvents ButtonRun As System.Windows.Forms.Button
    Friend WithEvents ColumnHeaderBytes As System.Windows.Forms.ColumnHeader
    Friend WithEvents ColumnHeaderMnemonic As System.Windows.Forms.ColumnHeader
    Friend WithEvents ColumnHeaderParameters As System.Windows.Forms.ColumnHeader
    Friend WithEvents ButtonReboot As System.Windows.Forms.Button
    Friend WithEvents ButtonDecIP As System.Windows.Forms.Button
    Friend WithEvents TextBoxBreakCS As System.Windows.Forms.TextBox
    Friend WithEvents ButtonForward As System.Windows.Forms.Button
    Friend WithEvents ButtonBack As System.Windows.Forms.Button
    Friend WithEvents CheckBoxTF As System.Windows.Forms.CheckBox
    Friend WithEvents ToolTipValueInfo As System.Windows.Forms.ToolTip
    Friend WithEvents TextBoxSearch As System.Windows.Forms.TextBox
    Friend WithEvents ButtonSearch As System.Windows.Forms.Button
    Friend WithEvents CheckBoxTextVideoMemory As System.Windows.Forms.CheckBox
    Friend WithEvents ButtonMemForward As System.Windows.Forms.Button
    Friend WithEvents ButtonMemBack As System.Windows.Forms.Button
    Friend WithEvents CheckBoxBytesOrChars As CheckBox
End Class
