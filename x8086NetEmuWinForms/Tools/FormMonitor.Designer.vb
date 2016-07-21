<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMonitor
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormMonitor))
        Me.gbFlags = New System.Windows.Forms.GroupBox()
        Me.chkTF = New System.Windows.Forms.CheckBox()
        Me.chkDF = New System.Windows.Forms.CheckBox()
        Me.chkIF = New System.Windows.Forms.CheckBox()
        Me.chkAF = New System.Windows.Forms.CheckBox()
        Me.chkPF = New System.Windows.Forms.CheckBox()
        Me.chkOF = New System.Windows.Forms.CheckBox()
        Me.chkSF = New System.Windows.Forms.CheckBox()
        Me.chkZF = New System.Windows.Forms.CheckBox()
        Me.chkCF = New System.Windows.Forms.CheckBox()
        Me.gbRegisters = New System.Windows.Forms.GroupBox()
        Me.btnForward = New System.Windows.Forms.Button()
        Me.btnBack = New System.Windows.Forms.Button()
        Me.btnDecIP = New System.Windows.Forms.Button()
        Me.txtSI = New System.Windows.Forms.TextBox()
        Me.txtDL = New System.Windows.Forms.TextBox()
        Me.txtES = New System.Windows.Forms.TextBox()
        Me.txtBP = New System.Windows.Forms.TextBox()
        Me.txtCL = New System.Windows.Forms.TextBox()
        Me.txtDH = New System.Windows.Forms.TextBox()
        Me.txtDI = New System.Windows.Forms.TextBox()
        Me.txtDS = New System.Windows.Forms.TextBox()
        Me.txtSP = New System.Windows.Forms.TextBox()
        Me.txtSS = New System.Windows.Forms.TextBox()
        Me.txtCH = New System.Windows.Forms.TextBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.txtBL = New System.Windows.Forms.TextBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.txtBH = New System.Windows.Forms.TextBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.txtIP = New System.Windows.Forms.TextBox()
        Me.txtCS = New System.Windows.Forms.TextBox()
        Me.txtAL = New System.Windows.Forms.TextBox()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.txtAH = New System.Windows.Forms.TextBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.lvStack = New System.Windows.Forms.ListView()
        Me.chAddress = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.chValue = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.btnStep = New System.Windows.Forms.Button()
        Me.lvCode = New System.Windows.Forms.ListView()
        Me.chCodeAddress = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.chBytes = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.chMnemonic = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.chParameters = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.btnRefresh = New System.Windows.Forms.Button()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.txtBreakIP = New System.Windows.Forms.TextBox()
        Me.txtMem = New System.Windows.Forms.TextBox()
        Me.txtMemSeg = New System.Windows.Forms.TextBox()
        Me.txtMemOff = New System.Windows.Forms.TextBox()
        Me.btnRun = New System.Windows.Forms.Button()
        Me.btnReboot = New System.Windows.Forms.Button()
        Me.txtBreakCS = New System.Windows.Forms.TextBox()
        Me.ttValueInfo = New System.Windows.Forms.ToolTip(Me.components)
        Me.CheckBoxTextVideoMemory = New System.Windows.Forms.CheckBox()
        Me.TextBoxSearch = New System.Windows.Forms.TextBox()
        Me.ButtonSearch = New System.Windows.Forms.Button()
        Me.ButtonMemForward = New System.Windows.Forms.Button()
        Me.ButtonMemBack = New System.Windows.Forms.Button()
        Me.CheckBoxBytesOrChars = New System.Windows.Forms.CheckBox()
        Me.gbFlags.SuspendLayout()
        Me.gbRegisters.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.SuspendLayout()
        '
        'gbFlags
        '
        Me.gbFlags.Controls.Add(Me.chkTF)
        Me.gbFlags.Controls.Add(Me.chkDF)
        Me.gbFlags.Controls.Add(Me.chkIF)
        Me.gbFlags.Controls.Add(Me.chkAF)
        Me.gbFlags.Controls.Add(Me.chkPF)
        Me.gbFlags.Controls.Add(Me.chkOF)
        Me.gbFlags.Controls.Add(Me.chkSF)
        Me.gbFlags.Controls.Add(Me.chkZF)
        Me.gbFlags.Controls.Add(Me.chkCF)
        Me.gbFlags.Location = New System.Drawing.Point(12, 12)
        Me.gbFlags.Name = "gbFlags"
        Me.gbFlags.Size = New System.Drawing.Size(103, 309)
        Me.gbFlags.TabIndex = 0
        Me.gbFlags.TabStop = False
        Me.gbFlags.Text = "Flags"
        '
        'chkTF
        '
        Me.chkTF.Appearance = System.Windows.Forms.Appearance.Button
        Me.chkTF.Location = New System.Drawing.Point(6, 259)
        Me.chkTF.Name = "chkTF"
        Me.chkTF.Size = New System.Drawing.Size(91, 24)
        Me.chkTF.TabIndex = 8
        Me.chkTF.Text = "TF: Trap"
        Me.chkTF.UseVisualStyleBackColor = True
        '
        'chkDF
        '
        Me.chkDF.Appearance = System.Windows.Forms.Appearance.Button
        Me.chkDF.Location = New System.Drawing.Point(6, 229)
        Me.chkDF.Name = "chkDF"
        Me.chkDF.Size = New System.Drawing.Size(91, 24)
        Me.chkDF.TabIndex = 7
        Me.chkDF.Text = "DF: Direction"
        Me.chkDF.UseVisualStyleBackColor = True
        '
        'chkIF
        '
        Me.chkIF.Appearance = System.Windows.Forms.Appearance.Button
        Me.chkIF.Location = New System.Drawing.Point(6, 199)
        Me.chkIF.Name = "chkIF"
        Me.chkIF.Size = New System.Drawing.Size(91, 24)
        Me.chkIF.TabIndex = 6
        Me.chkIF.Text = "IF: Interrupt"
        Me.chkIF.UseVisualStyleBackColor = True
        '
        'chkAF
        '
        Me.chkAF.Appearance = System.Windows.Forms.Appearance.Button
        Me.chkAF.Location = New System.Drawing.Point(6, 169)
        Me.chkAF.Name = "chkAF"
        Me.chkAF.Size = New System.Drawing.Size(91, 24)
        Me.chkAF.TabIndex = 5
        Me.chkAF.Text = "AF: Aux"
        Me.chkAF.UseVisualStyleBackColor = True
        '
        'chkPF
        '
        Me.chkPF.Appearance = System.Windows.Forms.Appearance.Button
        Me.chkPF.Location = New System.Drawing.Point(6, 139)
        Me.chkPF.Name = "chkPF"
        Me.chkPF.Size = New System.Drawing.Size(91, 24)
        Me.chkPF.TabIndex = 4
        Me.chkPF.Text = "PF: Parity"
        Me.chkPF.UseVisualStyleBackColor = True
        '
        'chkOF
        '
        Me.chkOF.Appearance = System.Windows.Forms.Appearance.Button
        Me.chkOF.Location = New System.Drawing.Point(6, 109)
        Me.chkOF.Name = "chkOF"
        Me.chkOF.Size = New System.Drawing.Size(91, 24)
        Me.chkOF.TabIndex = 3
        Me.chkOF.Text = "OF: Overflow"
        Me.chkOF.UseVisualStyleBackColor = True
        '
        'chkSF
        '
        Me.chkSF.Appearance = System.Windows.Forms.Appearance.Button
        Me.chkSF.Location = New System.Drawing.Point(6, 79)
        Me.chkSF.Name = "chkSF"
        Me.chkSF.Size = New System.Drawing.Size(91, 24)
        Me.chkSF.TabIndex = 2
        Me.chkSF.Text = "SF: Sign"
        Me.chkSF.UseVisualStyleBackColor = True
        '
        'chkZF
        '
        Me.chkZF.Appearance = System.Windows.Forms.Appearance.Button
        Me.chkZF.Location = New System.Drawing.Point(6, 49)
        Me.chkZF.Name = "chkZF"
        Me.chkZF.Size = New System.Drawing.Size(91, 24)
        Me.chkZF.TabIndex = 1
        Me.chkZF.Text = "ZF: Zero"
        Me.chkZF.UseVisualStyleBackColor = True
        '
        'chkCF
        '
        Me.chkCF.Appearance = System.Windows.Forms.Appearance.Button
        Me.chkCF.Location = New System.Drawing.Point(6, 19)
        Me.chkCF.Name = "chkCF"
        Me.chkCF.Size = New System.Drawing.Size(91, 24)
        Me.chkCF.TabIndex = 0
        Me.chkCF.Text = "CF: Carry"
        Me.chkCF.UseVisualStyleBackColor = True
        '
        'gbRegisters
        '
        Me.gbRegisters.Controls.Add(Me.btnForward)
        Me.gbRegisters.Controls.Add(Me.btnBack)
        Me.gbRegisters.Controls.Add(Me.btnDecIP)
        Me.gbRegisters.Controls.Add(Me.txtSI)
        Me.gbRegisters.Controls.Add(Me.txtDL)
        Me.gbRegisters.Controls.Add(Me.txtES)
        Me.gbRegisters.Controls.Add(Me.txtBP)
        Me.gbRegisters.Controls.Add(Me.txtCL)
        Me.gbRegisters.Controls.Add(Me.txtDH)
        Me.gbRegisters.Controls.Add(Me.txtDI)
        Me.gbRegisters.Controls.Add(Me.txtDS)
        Me.gbRegisters.Controls.Add(Me.txtSP)
        Me.gbRegisters.Controls.Add(Me.txtSS)
        Me.gbRegisters.Controls.Add(Me.txtCH)
        Me.gbRegisters.Controls.Add(Me.Label8)
        Me.gbRegisters.Controls.Add(Me.Label4)
        Me.gbRegisters.Controls.Add(Me.Label13)
        Me.gbRegisters.Controls.Add(Me.txtBL)
        Me.gbRegisters.Controls.Add(Me.Label7)
        Me.gbRegisters.Controls.Add(Me.Label12)
        Me.gbRegisters.Controls.Add(Me.Label3)
        Me.gbRegisters.Controls.Add(Me.Label10)
        Me.gbRegisters.Controls.Add(Me.Label11)
        Me.gbRegisters.Controls.Add(Me.txtBH)
        Me.gbRegisters.Controls.Add(Me.Label6)
        Me.gbRegisters.Controls.Add(Me.Label2)
        Me.gbRegisters.Controls.Add(Me.txtIP)
        Me.gbRegisters.Controls.Add(Me.txtCS)
        Me.gbRegisters.Controls.Add(Me.txtAL)
        Me.gbRegisters.Controls.Add(Me.Label9)
        Me.gbRegisters.Controls.Add(Me.txtAH)
        Me.gbRegisters.Controls.Add(Me.Label5)
        Me.gbRegisters.Controls.Add(Me.Label1)
        Me.gbRegisters.Location = New System.Drawing.Point(121, 12)
        Me.gbRegisters.Name = "gbRegisters"
        Me.gbRegisters.Size = New System.Drawing.Size(218, 309)
        Me.gbRegisters.TabIndex = 1
        Me.gbRegisters.TabStop = False
        Me.gbRegisters.Text = "Registers"
        '
        'btnForward
        '
        Me.btnForward.Location = New System.Drawing.Point(181, 88)
        Me.btnForward.Name = "btnForward"
        Me.btnForward.Size = New System.Drawing.Size(25, 23)
        Me.btnForward.TabIndex = 18
        Me.btnForward.Text = ">"
        Me.btnForward.UseVisualStyleBackColor = True
        '
        'btnBack
        '
        Me.btnBack.Location = New System.Drawing.Point(137, 89)
        Me.btnBack.Name = "btnBack"
        Me.btnBack.Size = New System.Drawing.Size(25, 23)
        Me.btnBack.TabIndex = 17
        Me.btnBack.Text = "<"
        Me.btnBack.UseVisualStyleBackColor = True
        '
        'btnDecIP
        '
        Me.btnDecIP.Location = New System.Drawing.Point(137, 117)
        Me.btnDecIP.Name = "btnDecIP"
        Me.btnDecIP.Size = New System.Drawing.Size(69, 23)
        Me.btnDecIP.TabIndex = 19
        Me.btnDecIP.Text = "<<"
        Me.btnDecIP.UseVisualStyleBackColor = True
        '
        'txtSI
        '
        Me.txtSI.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtSI.Location = New System.Drawing.Point(136, 210)
        Me.txtSI.Name = "txtSI"
        Me.txtSI.Size = New System.Drawing.Size(70, 26)
        Me.txtSI.TabIndex = 13
        Me.txtSI.Text = "0000"
        Me.txtSI.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtDL
        '
        Me.txtDL.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtDL.Location = New System.Drawing.Point(68, 114)
        Me.txtDL.Name = "txtDL"
        Me.txtDL.Size = New System.Drawing.Size(35, 26)
        Me.txtDL.TabIndex = 7
        Me.txtDL.Text = "00"
        Me.txtDL.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtES
        '
        Me.txtES.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtES.Location = New System.Drawing.Point(33, 242)
        Me.txtES.Name = "txtES"
        Me.txtES.Size = New System.Drawing.Size(70, 26)
        Me.txtES.TabIndex = 14
        Me.txtES.Text = "0000"
        Me.txtES.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtBP
        '
        Me.txtBP.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtBP.Location = New System.Drawing.Point(136, 274)
        Me.txtBP.Name = "txtBP"
        Me.txtBP.Size = New System.Drawing.Size(70, 26)
        Me.txtBP.TabIndex = 16
        Me.txtBP.Text = "0000"
        Me.txtBP.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtCL
        '
        Me.txtCL.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtCL.Location = New System.Drawing.Point(68, 82)
        Me.txtCL.Name = "txtCL"
        Me.txtCL.Size = New System.Drawing.Size(35, 26)
        Me.txtCL.TabIndex = 5
        Me.txtCL.Text = "00"
        Me.txtCL.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtDH
        '
        Me.txtDH.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtDH.Location = New System.Drawing.Point(33, 114)
        Me.txtDH.Name = "txtDH"
        Me.txtDH.Size = New System.Drawing.Size(35, 26)
        Me.txtDH.TabIndex = 6
        Me.txtDH.Text = "00"
        Me.txtDH.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtDI
        '
        Me.txtDI.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtDI.Location = New System.Drawing.Point(136, 242)
        Me.txtDI.Name = "txtDI"
        Me.txtDI.Size = New System.Drawing.Size(70, 26)
        Me.txtDI.TabIndex = 15
        Me.txtDI.Text = "0000"
        Me.txtDI.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtDS
        '
        Me.txtDS.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtDS.Location = New System.Drawing.Point(33, 210)
        Me.txtDS.Name = "txtDS"
        Me.txtDS.Size = New System.Drawing.Size(70, 26)
        Me.txtDS.TabIndex = 12
        Me.txtDS.Text = "0000"
        Me.txtDS.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtSP
        '
        Me.txtSP.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtSP.Location = New System.Drawing.Point(136, 178)
        Me.txtSP.Name = "txtSP"
        Me.txtSP.Size = New System.Drawing.Size(70, 26)
        Me.txtSP.TabIndex = 11
        Me.txtSP.Text = "0000"
        Me.txtSP.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtSS
        '
        Me.txtSS.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtSS.Location = New System.Drawing.Point(33, 178)
        Me.txtSS.Name = "txtSS"
        Me.txtSS.Size = New System.Drawing.Size(70, 26)
        Me.txtSS.TabIndex = 10
        Me.txtSS.Text = "0000"
        Me.txtSS.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtCH
        '
        Me.txtCH.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtCH.Location = New System.Drawing.Point(33, 82)
        Me.txtCH.Name = "txtCH"
        Me.txtCH.Size = New System.Drawing.Size(35, 26)
        Me.txtCH.TabIndex = 4
        Me.txtCH.Text = "00"
        Me.txtCH.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
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
        'txtBL
        '
        Me.txtBL.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtBL.Location = New System.Drawing.Point(68, 50)
        Me.txtBL.Name = "txtBL"
        Me.txtBL.Size = New System.Drawing.Size(35, 26)
        Me.txtBL.TabIndex = 3
        Me.txtBL.Text = "00"
        Me.txtBL.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
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
        'txtBH
        '
        Me.txtBH.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtBH.Location = New System.Drawing.Point(33, 50)
        Me.txtBH.Name = "txtBH"
        Me.txtBH.Size = New System.Drawing.Size(35, 26)
        Me.txtBH.TabIndex = 2
        Me.txtBH.Text = "00"
        Me.txtBH.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
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
        'txtIP
        '
        Me.txtIP.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtIP.Location = New System.Drawing.Point(136, 146)
        Me.txtIP.Name = "txtIP"
        Me.txtIP.Size = New System.Drawing.Size(70, 26)
        Me.txtIP.TabIndex = 9
        Me.txtIP.Text = "0000"
        Me.txtIP.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtCS
        '
        Me.txtCS.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtCS.Location = New System.Drawing.Point(33, 146)
        Me.txtCS.Name = "txtCS"
        Me.txtCS.Size = New System.Drawing.Size(70, 26)
        Me.txtCS.TabIndex = 8
        Me.txtCS.Text = "0000"
        Me.txtCS.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtAL
        '
        Me.txtAL.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtAL.Location = New System.Drawing.Point(68, 18)
        Me.txtAL.Name = "txtAL"
        Me.txtAL.Size = New System.Drawing.Size(35, 26)
        Me.txtAL.TabIndex = 1
        Me.txtAL.Text = "00"
        Me.txtAL.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
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
        'txtAH
        '
        Me.txtAH.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtAH.Location = New System.Drawing.Point(33, 18)
        Me.txtAH.Name = "txtAH"
        Me.txtAH.Size = New System.Drawing.Size(35, 26)
        Me.txtAH.TabIndex = 0
        Me.txtAH.Text = "00"
        Me.txtAH.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
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
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.lvStack)
        Me.GroupBox3.Location = New System.Drawing.Point(345, 12)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(200, 309)
        Me.GroupBox3.TabIndex = 2
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Stack"
        '
        'lvStack
        '
        Me.lvStack.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lvStack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lvStack.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.chAddress, Me.chValue})
        Me.lvStack.Font = New System.Drawing.Font("Consolas", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lvStack.FullRowSelect = True
        Me.lvStack.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None
        Me.lvStack.Location = New System.Drawing.Point(6, 18)
        Me.lvStack.Name = "lvStack"
        Me.lvStack.Size = New System.Drawing.Size(188, 285)
        Me.lvStack.TabIndex = 0
        Me.lvStack.UseCompatibleStateImageBehavior = False
        Me.lvStack.View = System.Windows.Forms.View.Details
        '
        'chAddress
        '
        Me.chAddress.Text = "Address"
        '
        'chValue
        '
        Me.chValue.Text = "Value"
        '
        'btnStep
        '
        Me.btnStep.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnStep.Location = New System.Drawing.Point(882, 604)
        Me.btnStep.Name = "btnStep"
        Me.btnStep.Size = New System.Drawing.Size(97, 32)
        Me.btnStep.TabIndex = 10
        Me.btnStep.Text = "Step"
        Me.btnStep.UseVisualStyleBackColor = True
        '
        'lvCode
        '
        Me.lvCode.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lvCode.CheckBoxes = True
        Me.lvCode.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.chCodeAddress, Me.chBytes, Me.chMnemonic, Me.chParameters})
        Me.lvCode.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lvCode.FullRowSelect = True
        Me.lvCode.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None
        Me.lvCode.HideSelection = False
        Me.lvCode.Location = New System.Drawing.Point(551, 18)
        Me.lvCode.Name = "lvCode"
        Me.lvCode.Size = New System.Drawing.Size(531, 580)
        Me.lvCode.TabIndex = 5
        Me.lvCode.UseCompatibleStateImageBehavior = False
        Me.lvCode.View = System.Windows.Forms.View.Details
        '
        'chCodeAddress
        '
        Me.chCodeAddress.Text = "Address"
        '
        'btnRefresh
        '
        Me.btnRefresh.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnRefresh.Location = New System.Drawing.Point(779, 604)
        Me.btnRefresh.Name = "btnRefresh"
        Me.btnRefresh.Size = New System.Drawing.Size(97, 32)
        Me.btnRefresh.TabIndex = 8
        Me.btnRefresh.Text = "Refresh"
        Me.btnRefresh.UseVisualStyleBackColor = True
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
        'txtBreakIP
        '
        Me.txtBreakIP.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtBreakIP.Location = New System.Drawing.Point(475, 327)
        Me.txtBreakIP.Name = "txtBreakIP"
        Me.txtBreakIP.Size = New System.Drawing.Size(70, 26)
        Me.txtBreakIP.TabIndex = 6
        Me.txtBreakIP.Text = "0000"
        Me.txtBreakIP.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtMem
        '
        Me.txtMem.BackColor = System.Drawing.SystemColors.Window
        Me.txtMem.Cursor = System.Windows.Forms.Cursors.Arrow
        Me.txtMem.Font = New System.Drawing.Font("Consolas", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtMem.Location = New System.Drawing.Point(12, 359)
        Me.txtMem.Multiline = True
        Me.txtMem.Name = "txtMem"
        Me.txtMem.ReadOnly = True
        Me.txtMem.Size = New System.Drawing.Size(533, 239)
        Me.txtMem.TabIndex = 7
        '
        'txtMemSeg
        '
        Me.txtMemSeg.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtMemSeg.Location = New System.Drawing.Point(12, 327)
        Me.txtMemSeg.Name = "txtMemSeg"
        Me.txtMemSeg.Size = New System.Drawing.Size(70, 26)
        Me.txtMemSeg.TabIndex = 3
        Me.txtMemSeg.Text = "0000"
        Me.txtMemSeg.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'txtMemOff
        '
        Me.txtMemOff.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtMemOff.Location = New System.Drawing.Point(88, 327)
        Me.txtMemOff.Name = "txtMemOff"
        Me.txtMemOff.Size = New System.Drawing.Size(70, 26)
        Me.txtMemOff.TabIndex = 4
        Me.txtMemOff.Text = "0000"
        Me.txtMemOff.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'btnRun
        '
        Me.btnRun.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnRun.Location = New System.Drawing.Point(985, 604)
        Me.btnRun.Name = "btnRun"
        Me.btnRun.Size = New System.Drawing.Size(97, 32)
        Me.btnRun.TabIndex = 11
        Me.btnRun.Text = "Run"
        Me.btnRun.UseVisualStyleBackColor = True
        '
        'btnReboot
        '
        Me.btnReboot.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnReboot.Location = New System.Drawing.Point(551, 604)
        Me.btnReboot.Name = "btnReboot"
        Me.btnReboot.Size = New System.Drawing.Size(97, 32)
        Me.btnReboot.TabIndex = 9
        Me.btnReboot.Text = "Reboot"
        Me.btnReboot.UseVisualStyleBackColor = True
        '
        'txtBreakCS
        '
        Me.txtBreakCS.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtBreakCS.Location = New System.Drawing.Point(399, 327)
        Me.txtBreakCS.Name = "txtBreakCS"
        Me.txtBreakCS.Size = New System.Drawing.Size(70, 26)
        Me.txtBreakCS.TabIndex = 5
        Me.txtBreakCS.Text = "0000"
        Me.txtBreakCS.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'ttValueInfo
        '
        Me.ttValueInfo.AutomaticDelay = 100
        Me.ttValueInfo.AutoPopDelay = 10000
        Me.ttValueInfo.InitialDelay = 100
        Me.ttValueInfo.IsBalloon = True
        Me.ttValueInfo.ReshowDelay = 20
        Me.ttValueInfo.UseAnimation = False
        Me.ttValueInfo.UseFading = False
        '
        'CheckBoxTextVideoMemory
        '
        Me.CheckBoxTextVideoMemory.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.CheckBoxTextVideoMemory.AutoSize = True
        Me.CheckBoxTextVideoMemory.Location = New System.Drawing.Point(345, 614)
        Me.CheckBoxTextVideoMemory.Name = "CheckBoxTextVideoMemory"
        Me.CheckBoxTextVideoMemory.Size = New System.Drawing.Size(15, 14)
        Me.CheckBoxTextVideoMemory.TabIndex = 14
        Me.ttValueInfo.SetToolTip(Me.CheckBoxTextVideoMemory, "Enable Text Video Memory compatibility search")
        Me.CheckBoxTextVideoMemory.UseVisualStyleBackColor = True
        '
        'TextBoxSearch
        '
        Me.TextBoxSearch.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.TextBoxSearch.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBoxSearch.Location = New System.Drawing.Point(12, 607)
        Me.TextBoxSearch.Name = "TextBoxSearch"
        Me.TextBoxSearch.Size = New System.Drawing.Size(327, 26)
        Me.TextBoxSearch.TabIndex = 12
        '
        'ButtonSearch
        '
        Me.ButtonSearch.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonSearch.Location = New System.Drawing.Point(366, 604)
        Me.ButtonSearch.Name = "ButtonSearch"
        Me.ButtonSearch.Size = New System.Drawing.Size(97, 32)
        Me.ButtonSearch.TabIndex = 13
        Me.ButtonSearch.Text = "Search"
        Me.ButtonSearch.UseVisualStyleBackColor = True
        '
        'ButtonMemForward
        '
        Me.ButtonMemForward.Location = New System.Drawing.Point(195, 329)
        Me.ButtonMemForward.Name = "ButtonMemForward"
        Me.ButtonMemForward.Size = New System.Drawing.Size(25, 23)
        Me.ButtonMemForward.TabIndex = 20
        Me.ButtonMemForward.Text = ">"
        Me.ButtonMemForward.UseVisualStyleBackColor = True
        '
        'ButtonMemBack
        '
        Me.ButtonMemBack.Location = New System.Drawing.Point(164, 329)
        Me.ButtonMemBack.Name = "ButtonMemBack"
        Me.ButtonMemBack.Size = New System.Drawing.Size(25, 23)
        Me.ButtonMemBack.TabIndex = 19
        Me.ButtonMemBack.Text = "<"
        Me.ButtonMemBack.UseVisualStyleBackColor = True
        '
        'CheckBoxBytesOrChars
        '
        Me.CheckBoxBytesOrChars.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.CheckBoxBytesOrChars.AutoSize = True
        Me.CheckBoxBytesOrChars.Location = New System.Drawing.Point(758, 614)
        Me.CheckBoxBytesOrChars.Name = "CheckBoxBytesOrChars"
        Me.CheckBoxBytesOrChars.Size = New System.Drawing.Size(15, 14)
        Me.CheckBoxBytesOrChars.TabIndex = 21
        Me.CheckBoxBytesOrChars.UseVisualStyleBackColor = True
        '
        'FormMonitor
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1094, 648)
        Me.Controls.Add(Me.CheckBoxBytesOrChars)
        Me.Controls.Add(Me.ButtonMemForward)
        Me.Controls.Add(Me.ButtonMemBack)
        Me.Controls.Add(Me.CheckBoxTextVideoMemory)
        Me.Controls.Add(Me.ButtonSearch)
        Me.Controls.Add(Me.TextBoxSearch)
        Me.Controls.Add(Me.btnReboot)
        Me.Controls.Add(Me.txtMem)
        Me.Controls.Add(Me.Label14)
        Me.Controls.Add(Me.lvCode)
        Me.Controls.Add(Me.btnRun)
        Me.Controls.Add(Me.btnRefresh)
        Me.Controls.Add(Me.btnStep)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.gbRegisters)
        Me.Controls.Add(Me.gbFlags)
        Me.Controls.Add(Me.txtBreakCS)
        Me.Controls.Add(Me.txtBreakIP)
        Me.Controls.Add(Me.txtMemSeg)
        Me.Controls.Add(Me.txtMemOff)
        Me.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.KeyPreview = True
        Me.MinimumSize = New System.Drawing.Size(1095, 680)
        Me.Name = "FormMonitor"
        Me.Text = "Monitor"
        Me.gbFlags.ResumeLayout(False)
        Me.gbRegisters.ResumeLayout(False)
        Me.gbRegisters.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents gbFlags As System.Windows.Forms.GroupBox
    Friend WithEvents chkOF As System.Windows.Forms.CheckBox
    Friend WithEvents chkSF As System.Windows.Forms.CheckBox
    Friend WithEvents chkZF As System.Windows.Forms.CheckBox
    Friend WithEvents chkCF As System.Windows.Forms.CheckBox
    Friend WithEvents chkAF As System.Windows.Forms.CheckBox
    Friend WithEvents chkPF As System.Windows.Forms.CheckBox
    Friend WithEvents chkDF As System.Windows.Forms.CheckBox
    Friend WithEvents chkIF As System.Windows.Forms.CheckBox
    Friend WithEvents gbRegisters As System.Windows.Forms.GroupBox
    Friend WithEvents txtAL As System.Windows.Forms.TextBox
    Friend WithEvents txtAH As System.Windows.Forms.TextBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents txtBL As System.Windows.Forms.TextBox
    Friend WithEvents txtBH As System.Windows.Forms.TextBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtDL As System.Windows.Forms.TextBox
    Friend WithEvents txtCL As System.Windows.Forms.TextBox
    Friend WithEvents txtDH As System.Windows.Forms.TextBox
    Friend WithEvents txtCH As System.Windows.Forms.TextBox
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents txtSI As System.Windows.Forms.TextBox
    Friend WithEvents txtBP As System.Windows.Forms.TextBox
    Friend WithEvents txtSS As System.Windows.Forms.TextBox
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents txtCS As System.Windows.Forms.TextBox
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents txtSP As System.Windows.Forms.TextBox
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents txtIP As System.Windows.Forms.TextBox
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents txtES As System.Windows.Forms.TextBox
    Friend WithEvents txtDI As System.Windows.Forms.TextBox
    Friend WithEvents txtDS As System.Windows.Forms.TextBox
    Friend WithEvents Label13 As System.Windows.Forms.Label
    Friend WithEvents Label12 As System.Windows.Forms.Label
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
    Friend WithEvents lvStack As System.Windows.Forms.ListView
    Friend WithEvents chAddress As System.Windows.Forms.ColumnHeader
    Friend WithEvents chValue As System.Windows.Forms.ColumnHeader
    Friend WithEvents btnStep As System.Windows.Forms.Button
    Friend WithEvents lvCode As System.Windows.Forms.ListView
    Friend WithEvents chCodeAddress As System.Windows.Forms.ColumnHeader
    Friend WithEvents btnRefresh As System.Windows.Forms.Button
    Friend WithEvents Label14 As System.Windows.Forms.Label
    Friend WithEvents txtBreakIP As System.Windows.Forms.TextBox
    Friend WithEvents txtMem As System.Windows.Forms.TextBox
    Friend WithEvents txtMemSeg As System.Windows.Forms.TextBox
    Friend WithEvents txtMemOff As System.Windows.Forms.TextBox
    Friend WithEvents btnRun As System.Windows.Forms.Button
    Friend WithEvents chBytes As System.Windows.Forms.ColumnHeader
    Friend WithEvents chMnemonic As System.Windows.Forms.ColumnHeader
    Friend WithEvents chParameters As System.Windows.Forms.ColumnHeader
    Friend WithEvents btnReboot As System.Windows.Forms.Button
    Friend WithEvents btnDecIP As System.Windows.Forms.Button
    Friend WithEvents txtBreakCS As System.Windows.Forms.TextBox
    Friend WithEvents btnForward As System.Windows.Forms.Button
    Friend WithEvents btnBack As System.Windows.Forms.Button
    Friend WithEvents chkTF As System.Windows.Forms.CheckBox
    Friend WithEvents ttValueInfo As System.Windows.Forms.ToolTip
    Friend WithEvents TextBoxSearch As System.Windows.Forms.TextBox
    Friend WithEvents ButtonSearch As System.Windows.Forms.Button
    Friend WithEvents CheckBoxTextVideoMemory As System.Windows.Forms.CheckBox
    Friend WithEvents ButtonMemForward As System.Windows.Forms.Button
    Friend WithEvents ButtonMemBack As System.Windows.Forms.Button
    Friend WithEvents CheckBoxBytesOrChars As CheckBox
End Class
