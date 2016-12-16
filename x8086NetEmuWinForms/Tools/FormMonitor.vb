Imports System.Threading
Imports x8086NetEmu
Imports System.Text

Public Class FormMonitor
    Private Structure Breakpoint
        Public Segment As Integer
        Public Offset As Integer

        Public Sub New(segment As Integer, offset As Integer)
            Me.Segment = segment
            Me.Offset = offset
        End Sub
    End Structure

    Public Class State
        Public Registers As x8086.GPRegisters
        Public Flags As x8086.GPFlags
        'Public RAM(x8086.ROMStart - 1) As Byte

        Private cpu As x8086
        Private includeRAM As Boolean

        Public Sub New(cpu As x8086, includeRAM As Boolean)
            Me.cpu = cpu

            Registers = cpu.Registers.Clone()
            Flags = cpu.Flags.Clone()

            'If includeRAM Then Array.Copy(cpu.Memory, RAM, RAM.Length)
        End Sub

        Public Sub Restore()
            cpu.Registers = Registers
            cpu.Flags = Flags

            'If includeRAM Then Array.Copy(RAM, cpu.Memory, RAM.Length)
        End Sub
    End Class

    Private Class EvaluateResult
        Public Property Value As Integer
        Public Property IsValid As Boolean

        Public Sub New()
            IsValid = True
        End Sub

        Public Sub New(value As Integer, valid As Boolean)
            Me.New()
            Me.Value = value
            Me.IsValid = valid
        End Sub
    End Class

    Private history(2 ^ 18) As State
    Private historyPointer As Integer

    Private mEmulator As x8086
    Private currentCSIP As String
    Private currentSSSP As String
    Private ignoreEvents As Boolean
    Private isInit As Boolean
    Private breakIP As Integer = -1
    Private breakCS As Integer = -1
    Private breakPoints As List(Of Breakpoint) = New List(Of Breakpoint)
    Private isRunning As Boolean
    Private baseCS As Integer
    Private baseIP As Integer

    Private loopWaiter As AutoResetEvent
    Private threadLoop As Thread

    Private ohpWaiter As AutoResetEvent
    Private ohpThreadLoop As Thread
    Private offsetHistoryDirection As Integer = 0

    Private abortThreads As Boolean

    Private Const numberSufixes As String = "hbo" ' hEX / bINARY / oCtal
    Private activeInstruction As x8086.Instruction

    Private navigator As System.Xml.XPath.XPathNavigator = New System.Xml.XPath.XPathDocument(New IO.StringReader("<r/>")).CreateNavigator()
    Private rex As System.Text.RegularExpressions.Regex = New System.Text.RegularExpressions.Regex("([\+\-\*])")
    Private Evaluator As Func(Of String, Double) = Function(exp) CDbl(navigator.Evaluate("number(" + rex.Replace(exp, " ${1} ").Replace("/", " div ").Replace("%", " mod ") + ")"))

#Region "Controls Event Handlers"
    Private Sub frmMonitor_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        InitLV(lvStack)
        AutoSizeLastColumn(lvStack)

        InitLV(ListViewCode)
        AutoSizeLastColumn(ListViewCode)
        ListViewCode.BackColor = Color.FromArgb(34, 40, 42)
        ListViewCode.ForeColor = Color.FromArgb(102, 80, 15)

        loopWaiter = New AutoResetEvent(False)
        ohpWaiter = New AutoResetEvent(False)
        ohpThreadLoop = New Thread(AddressOf OffsetHistoryLoopSub)
        ohpThreadLoop.Start()

        'txtBreakCS.Text = "F600"
        'txtBreakIP.Text = "0F1E"
        txtBreakCS.Text = "0000"
        txtBreakIP.Text = "0000"

        SetupControls(Me)
        SetupCheckBoxes()
    End Sub

    Private Sub frmMonitor_FormClosing(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        isInit = False
        ignoreEvents = True
        abortThreads = True

        ohpWaiter.Set()
        loopWaiter.Set()

        If isRunning Then
            e.Cancel = True
        Else
            mEmulator.DebugMode = False
        End If
    End Sub

    Private Sub frmMonitor_KeyDown(sender As Object, e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.F5
                StartStopRunMode()
            Case Keys.F8
                StepInto()
        End Select
    End Sub

    Private Sub btnStep_Click(sender As System.Object, e As System.EventArgs) Handles btnStep.Click
        StepInto()
    End Sub

    Private Sub lvCode_DoubleClick(sender As Object, e As System.EventArgs) Handles ListViewCode.DoubleClick
        If ListViewCode.SelectedItems.Count = 0 Then Exit Sub
        Dim address As String = ListViewCode.SelectedItems(0).Text
        txtCS.Text = address.Split(":")(0)
        txtIP.Text = address.Split(":")(1)
    End Sub

    Private Sub lvCode_ItemChecked(sender As Object, e As System.Windows.Forms.ItemCheckedEventArgs) Handles ListViewCode.ItemChecked
        If e.Item.Text = "" OrElse e.Item.SubItems.Count <> 4 Then Exit Sub

        Dim segment As Integer = (Val("&h" + e.Item.Text.Split(":")(0)) And &HFFFF)
        Dim offset As Integer = (Val("&h" + e.Item.Text.Split(":")(1)) And &HFFFF)

        For Each bp In breakPoints
            If bp.Offset = offset AndAlso bp.Segment = segment Then
                breakPoints.Remove(bp)
                Exit For
            End If
        Next

        If e.Item.Checked Then
            breakPoints.Add(New Breakpoint(segment, offset))
            e.Item.BackColor = Color.FromArgb(127, 54, 64)
        Else
            e.Item.BackColor = ListViewCode.BackColor
        End If
        e.Item.SubItems(1).BackColor = e.Item.BackColor
        e.Item.SubItems(2).BackColor = e.Item.BackColor
        e.Item.SubItems(3).BackColor = e.Item.BackColor
    End Sub

    Private Sub btnRun_Click(sender As System.Object, e As System.EventArgs) Handles btnRun.Click
        StartStopRunMode()
    End Sub

    Private Sub lvCode_ClientSizeChanged(sender As Object, e As System.EventArgs) Handles ListViewCode.ClientSizeChanged
        AutoSizeLastColumn(ListViewCode)
    End Sub

    Private Sub btnRefresh_Click(sender As System.Object, e As System.EventArgs) Handles btnRefresh.Click
        RefreshCodeListing()
    End Sub

    Private Sub btnReboot_Click(sender As System.Object, e As System.EventArgs) Handles btnReboot.Click
        Emulator.HardReset()
        historyPointer = -1
        RefreshCodeListing()
    End Sub

    Private Sub btnDecIP_Click(sender As System.Object, e As System.EventArgs) Handles btnDecIP.Click
        Dim IP As Integer = Val("&h" + txtIP.Text) And &HFFFF
        Dim CS As Integer = Val("&h" + txtCS.Text) And &HFFFF

        For i As Integer = 1 To 7
            Dim previous = Emulator.Decode(CS, IP - i)
            If previous.IsValid AndAlso previous.Size = i Then
                If Emulator.Decode(CS, IP - i + previous.Size) = activeInstruction Then
                    txtIP.Text = (IP - 1).ToHex(x8086.DataSize.Word, "")
                    Exit For
                End If
            End If
        Next
    End Sub

    Private Sub btnBack_MouseDown(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles btnBack.MouseDown
        If e.Button = Windows.Forms.MouseButtons.Left Then
            offsetHistoryDirection = -1
        ElseIf e.Button = Windows.Forms.MouseButtons.Right Then
            offsetHistoryDirection = -100
        End If
    End Sub

    Private Sub btnBack_MouseUp(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles btnBack.MouseUp
        offsetHistoryDirection = 0
    End Sub

    Private Sub btnForward_MouseDown(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles btnForward.MouseDown
        If e.Button = Windows.Forms.MouseButtons.Left Then
            offsetHistoryDirection = 1
        ElseIf e.Button = Windows.Forms.MouseButtons.Right Then
            offsetHistoryDirection = 100
        End If
    End Sub

    Private Sub btnForward_MouseUp(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles btnForward.MouseUp
        offsetHistoryDirection = 0
    End Sub

#End Region

    Public Property Emulator As x8086
        Get
            Return mEmulator
        End Get
        Set(value As x8086)
            mEmulator = value
            AddHandler mEmulator.InstructionDecoded, Sub()
                                                         If Not (ignoreEvents OrElse isRunning) Then UpdateUI()
                                                         loopWaiter.Set()
                                                     End Sub
            AddHandler mEmulator.EmulationTerminated, Sub() If isRunning Then StartStopRunMode()
            isInit = True

            StepInto()
        End Set
    End Property

    Private Sub UpdateUI(Optional doFastUpdate As Boolean = False)
        If ignoreEvents OrElse Not isInit Then Exit Sub

        Me.Invoke(New MethodInvoker(Sub()
                                        UpdateMemory()
                                        GenCodeAhead()
                                        UpdateFlagsAndRegisters()

                                        If Not doFastUpdate Then
                                            SetTextBoxesState(Me)
                                            UpdateStack()
                                        End If
                                    End Sub))
    End Sub

    Private Sub SetTextBoxesState(container As Control)
        For Each ctrl As Control In container.Controls
            If TypeOf ctrl Is GroupBox Then
                SetTextBoxesState(ctrl)
            Else
                If TypeOf ctrl Is TextBox Then
                    If Emulator.Registers.ActiveSegmentRegister.ToString() = ctrl.Name.Substring(3) Then
                        ctrl.ForeColor = Color.Blue 
                    ElseIf ctrl.Name <> txtMem.Name Then
                        ctrl.ForeColor = Color.Black
                    End If
                    ctrl.Refresh()
                End If
            End If
        Next
    End Sub

    Private Sub UpdateMemory()
        If Not isInit Then Exit Sub

        Try
            Dim address As Integer = x8086.SegOffToAbs(EvaluateExpression(txtMemSeg.Text).Value, EvaluateExpression(txtMemOff.Text).Value)

            Dim b As Byte
            Dim res As String = ""
            For i As Integer = 0 To 16 * 15 Step 16
                Dim mem As String = ""
                Dim bcr As String = "    "
                For k = 0 To 15
                    b = mEmulator.RAM(address + i + k)
                    If k = 8 Then mem += "- "
                    mem += b.ToHex("") + " "
                    If b <= 31 OrElse b > 122 Then
                        bcr += "."
                    Else
                        bcr += Chr(b)
                    End If
                Next
                res += mem + bcr + vbCrLf
            Next
            txtMem.Text = res
        Catch
        End Try
    End Sub

    Private Function StringToRegister(value As String) As x8086.GPRegisters.RegistersTypes
        Select Case value.ToUpper()
            Case "AL" : Return x8086.GPRegisters.RegistersTypes.AL
            Case "AH" : Return x8086.GPRegisters.RegistersTypes.AH
            Case "AX" : Return x8086.GPRegisters.RegistersTypes.AX

            Case "BL" : Return x8086.GPRegisters.RegistersTypes.BL
            Case "BH" : Return x8086.GPRegisters.RegistersTypes.BH
            Case "BX" : Return x8086.GPRegisters.RegistersTypes.BX

            Case "CL" : Return x8086.GPRegisters.RegistersTypes.CL
            Case "CH" : Return x8086.GPRegisters.RegistersTypes.CH
            Case "CX" : Return x8086.GPRegisters.RegistersTypes.CX

            Case "DL" : Return x8086.GPRegisters.RegistersTypes.DL
            Case "DH" : Return x8086.GPRegisters.RegistersTypes.DH
            Case "DX" : Return x8086.GPRegisters.RegistersTypes.DX

            Case "CS" : Return x8086.GPRegisters.RegistersTypes.CS
            Case "IP" : Return x8086.GPRegisters.RegistersTypes.IP
            Case "SS" : Return x8086.GPRegisters.RegistersTypes.SS
            Case "SP" : Return x8086.GPRegisters.RegistersTypes.SP
            Case "BP" : Return x8086.GPRegisters.RegistersTypes.BP
            Case "SI" : Return x8086.GPRegisters.RegistersTypes.SI
            Case "DI" : Return x8086.GPRegisters.RegistersTypes.DI
            Case "DS" : Return x8086.GPRegisters.RegistersTypes.DS
            Case "ES" : Return x8086.GPRegisters.RegistersTypes.ES

            Case "AS" : Return mEmulator.Registers.Val(mEmulator.Registers.ActiveSegmentRegister)

            Case Else
                Return x8086.GPRegisters.RegistersTypes.NONE
        End Select
    End Function

    Private Function EvaluateExpression(value As String) As EvaluateResult
        If value = "" Then Return New EvaluateResult()
        Dim result As Integer = 0

        If value.Contains("AS") Then
            value = value.Replace("AS", mEmulator.Registers.Val(mEmulator.Registers.ActiveSegmentRegister).ToString() + "d")
        Else
            For Each reg In [Enum].GetNames(GetType(x8086.GPRegisters.RegistersTypes))
                If value.Contains(reg) Then
                    value = value.Replace(reg, mEmulator.Registers.Val(StringToRegister(reg)).ToString() + "d")
                End If
            Next
        End If

        Dim GetNumber = Function(s As String, p As Integer)
                            Dim i As Integer
                            Dim n As String = ""
                            For i = p - 1 To 0 Step -1
                                If Not Char.IsLetterOrDigit(s(i)) Then Exit For
                                n = s(i) + n
                            Next
                            Dim r As Integer
                            If Binary.TryParse(n, r) Then
                                Return {r, i, True}
                            Else
                                Return {r, i, False}
                            End If
                        End Function

        Dim IsSpecialLetter = Function(s As Char)
                                  If Char.IsLetter(s) AndAlso Char.IsLower(s) Then
                                      Return s = "h" OrElse s = "b" OrElse s = "d" OrElse s = "o"
                                  Else
                                      Return False
                                  End If
                              End Function

        Dim properFormat As Boolean = IsSpecialLetter(value.Last())
        If value.Length = 1 Then
            If Not properFormat Then value += "h"
        ElseIf Not properFormat AndAlso Char.IsLetterOrDigit(value.Last()) Then
            value += "h"
        End If
        value += " "

        Dim isDone As Boolean
        Do
            isDone = True
            For i As Integer = 0 To value.Length - 1
                If Not Char.IsLetterOrDigit(value(i)) AndAlso i > 0 Then
                    If IsSpecialLetter(value(i - 1)) Then
                        Dim data = GetNumber(value, i)
                        If data(2) = True Then
                            value = value.Substring(0, data(1) + 1) + data(0).ToString() + value.Substring(i)
                            isDone = False
                            Exit For
                        End If
                    End If
                End If
            Next
        Loop Until isDone

        Try
            result = Evaluator(value)
            Return New EvaluateResult(result, True)
        Catch
            Return New EvaluateResult(result, False)
        End Try
    End Function

    Private Sub UpdateFlagsAndRegisters()
        Dim oldIgnoreEvents As Boolean = ignoreEvents
        ignoreEvents = True

        With mEmulator
            With .Registers
                If Not txtAH.Focused Then txtAH.Text = .AH.ToHex(x8086.DataSize.Byte, "")
                If Not txtAL.Focused Then txtAL.Text = .AL.ToHex(x8086.DataSize.Byte, "")

                If Not txtBH.Focused Then txtBH.Text = .BH.ToHex(x8086.DataSize.Byte, "")
                If Not txtBL.Focused Then txtBL.Text = .BL.ToHex(x8086.DataSize.Byte, "")

                If Not txtCH.Focused Then txtCH.Text = .CH.ToHex(x8086.DataSize.Byte, "")
                If Not txtCL.Focused Then txtCL.Text = .CL.ToHex(x8086.DataSize.Byte, "")

                If Not txtDH.Focused Then txtDH.Text = .DH.ToHex(x8086.DataSize.Byte, "")
                If Not txtDL.Focused Then txtDL.Text = .DL.ToHex(x8086.DataSize.Byte, "")

                If Not txtCS.Focused Then txtCS.Text = .CS.ToHex(x8086.DataSize.Word, "")
                If Not txtIP.Focused Then txtIP.Text = .IP.ToHex(x8086.DataSize.Word, "")

                If Not txtSS.Focused Then txtSS.Text = .SS.ToHex(x8086.DataSize.Word, "")
                If Not txtSP.Focused Then txtSP.Text = .SP.ToHex(x8086.DataSize.Word, "")

                If Not txtBP.Focused Then txtBP.Text = .BP.ToHex(x8086.DataSize.Word, "")
                If Not txtSI.Focused Then txtSI.Text = .SI.ToHex(x8086.DataSize.Word, "")

                If Not txtDS.Focused Then txtDS.Text = .DS.ToHex(x8086.DataSize.Word, "")
                If Not txtDI.Focused Then txtDI.Text = .DI.ToHex(x8086.DataSize.Word, "")

                If Not txtES.Focused Then txtES.Text = .ES.ToHex(x8086.DataSize.Word, "")
            End With

            With Emulator.Flags
                chkAF.Checked = (.AF = 1)
                chkCF.Checked = (.CF = 1)
                chkDF.Checked = (.DF = 1)
                chkIF.Checked = (.IF = 1)
                chkOF.Checked = (.OF = 1)
                chkPF.Checked = (.PF = 1)
                chkSF.Checked = (.SF = 1)
                chkZF.Checked = (.ZF = 1)
                chkTF.Checked = (.TF = 1)
            End With
        End With

        ignoreEvents = oldIgnoreEvents
    End Sub

    Private Sub UpdateStack()
        Dim index As Integer = 0

        If lvStack.Items.ContainsKey(currentSSSP) Then
            With lvStack.Items(currentSSSP)
                .BackColor = lvStack.BackColor
                .SubItems(1).BackColor = lvStack.BackColor
            End With
        End If

        With Emulator
            currentSSSP = x8086.SegOffToAbs(.Registers.SS, .Registers.SP).ToString("X")

            Dim offset As Integer = 0
            If .Registers.SP Mod 2 = 0 Then offset = 1

            Dim startOffset As Integer = Math.Min(Math.Max(.Registers.SP + 0, .Registers.SP + 128), &HFFFF - offset)
            Dim endOffset As Integer = Math.Max(Math.Min(.Registers.SP + 0, .Registers.SP - 128), 0 + offset)

            For ptr As Integer = startOffset To endOffset Step -2
                Dim address As String = x8086.SegOffToAbs(.Registers.SS, ptr).ToString("X")
                Dim value As Integer = .RAM16(.Registers.SS, ptr)

                Dim item As ListViewItem
                If index < lvStack.Items.Count Then
                    item = lvStack.Items(index)
                Else
                    item = lvStack.Items.Add(address, "", 0)
                    item.SubItems.Add("")
                End If
                item.Text = .Registers.SS.ToHex(x8086.DataSize.Word, "") + ":" + ptr.ToHex(x8086.DataSize.Word, "")
                item.SubItems(1).Text = value.ToHex(x8086.DataSize.Word, "")
                If ptr = .Registers.SP Then
                    item.BackColor = Color.DarkSlateBlue
                    item.EnsureVisible()
                End If

                index += 1
            Next
        End With

        Do While lvStack.Items.Count > index
            lvStack.Items.RemoveAt(lvStack.Items.Count - 1)
        Loop
    End Sub

    Private Sub GenCodeAhead()
        Dim item As ListViewItem
        Dim CS As Integer = mEmulator.Registers.CS
        Dim IP As Integer = mEmulator.Registers.IP
        Dim insIndex As Integer
        Dim insertedCount As Integer
        Dim newCount As Integer

        If ListViewCode.Items.ContainsKey(currentCSIP) Then
            With ListViewCode.Items(currentCSIP)
                .BackColor = ListViewCode.BackColor
                .SubItems(1).BackColor = ListViewCode.BackColor
                .SubItems(2).BackColor = ListViewCode.BackColor
                .SubItems(3).BackColor = ListViewCode.BackColor
            End With
        End If

        currentCSIP = x8086.SegOffToAbs(CS, IP).ToString("X")
        ignoreEvents = True
        Do
            Dim address As String = x8086.SegOffToAbs(CS, IP).ToString("X")

            insIndex = -1
            If ListViewCode.Items.ContainsKey(address) Then
                item = ListViewCode.Items(address)
                insertedCount += 1
            Else
                For Each sItem As ListViewItem In ListViewCode.Items
                    If sItem.Tag > address Then
                        insIndex = sItem.Index
                        Exit For
                    End If
                Next
                If insIndex <> -1 Then
                    item = ListViewCode.Items.Insert(insIndex, address, "", 0)
                    insertedCount += 1
                Else
                    item = ListViewCode.Items.Add(address, "", 0)
                    newCount += 1
                End If
                item.SubItems.Add("")
                item.SubItems.Add("")
                item.SubItems.Add("")
                item.Tag = address
                item.UseItemStyleForSubItems = False

                item.ForeColor = ListViewCode.ForeColor
                item.SubItems(1).ForeColor = Color.FromArgb(88, 81, 64)
                item.SubItems(2).ForeColor = Color.FromArgb(97, 175, 99)
                item.SubItems(3).ForeColor = Color.FromArgb(35 + 20, 87 + 20, 140 + 20)
            End If

            If Emulator.IsExecuting Then Exit Do
            Dim info As x8086.Instruction = mEmulator.Decode(CS, IP)
            If Not info.IsValid Then Exit Do

            Dim curIP As String = IP.ToHex(x8086.DataSize.Word, "")
            If CInt(IP) + info.Size > &HFFFF Then Exit Do
            IP = (IP + info.Size) Mod &HFFFF

            If Not isRunning OrElse item.Text = "" Then
                item.Text = info.CS.ToHex(x8086.DataSize.Word, "") + ":" + info.IP.ToHex(x8086.DataSize.Word, "")
                item.SubItems(1).Text = GetBytesString(info.Bytes)
                item.SubItems(2).Text = info.Mnemonic
                If info.Message = "" Then
                    If info.Parameter2 = "" Then
                        item.SubItems(3).Text = info.Parameter1
                    Else
                        item.SubItems(3).Text = info.Parameter1 + ", " + info.Parameter2
                    End If
                Else
                    item.SubItems(3).Text = info.Message
                End If
            End If

            If address = currentCSIP Then
                item.BackColor = Color.FromArgb(197, 199, 192)
                item.SubItems(1).BackColor = item.BackColor
                item.SubItems(2).BackColor = item.BackColor
                item.SubItems(3).BackColor = item.BackColor
                item.EnsureVisible()
                activeInstruction = info
            ElseIf item.BackColor <> ListViewCode.BackColor AndAlso Not item.Checked Then
                item.SubItems(1).BackColor = item.BackColor
                item.SubItems(2).BackColor = item.BackColor
                item.SubItems(3).BackColor = item.BackColor
            End If
        Loop Until (newCount >= 100) OrElse (insertedCount >= 100) OrElse (ListViewCode.Items.Count >= 1000)

        ignoreEvents = False
    End Sub

    Private Sub StepInto()
        If Not mEmulator.DebugMode Then mEmulator.DebugMode = True

        If isRunning Then
            StartStopRunMode()
        Else
            DoStep()
        End If
    End Sub

    Private Sub RefreshCodeListing()
        lvStack.Items.Clear()
        ListViewCode.Items.Clear()
        UpdateUI()
    End Sub

    Private Sub StartStopRunMode()
        If Not mEmulator.DebugMode Then mEmulator.DebugMode = True

        If isRunning Then
            abortThreads = True
        Else
            abortThreads = False

            threadLoop = New Thread(AddressOf RunLoop)
            threadLoop.Start()
        End If
    End Sub

    Private Sub DoStep()
        If Not mEmulator.IsHalted Then
            If historyPointer = history.Length - 1 Then
                Array.Copy(history, 1, history, 0, history.Length - 1)
            Else
                historyPointer += 1
            End If
            history(historyPointer) = New State(mEmulator, True)
        End If

        mEmulator.StepInto()
    End Sub

    Private Sub RunLoop()
        isRunning = True
        ignoreEvents = True

        Dim count As Integer = 0
        Dim maxSteps As Integer = 5000
        Dim address As Integer
        Dim lastAddress As Integer = -1
        'Dim instructions As New List(Of x8086.Instruction)

        Do
            DoStep()
            loopWaiter.WaitOne()

            If mEmulator.IsHalted Then StartStopRunMode()

            'Dim instruction = mEmulator.Decode(mEmulator)
            'If Not instructions.Contains(instruction) Then instructions.Add(instruction)

            If breakIP = mEmulator.Registers.IP AndAlso breakCS = mEmulator.Registers.CS Then
                Beep()
                abortThreads = True
                Continue Do
            End If

            For Each bp In breakPoints
                If bp.Offset = mEmulator.Registers.IP AndAlso bp.Segment = mEmulator.Registers.CS Then
                    Beep()
                    abortThreads = True
                    Continue Do
                End If
            Next

            address = x8086.SegOffToAbs(mEmulator.Registers.CS, mEmulator.Registers.IP)

            If count = 0 Then
                ignoreEvents = False
                UpdateUI()
                ignoreEvents = True
                count = maxSteps
            Else
                count -= 1
            End If
        Loop Until abortThreads

        If Not abortThreads Then UpdateUI()

        isRunning = False
        abortThreads = False
        ignoreEvents = False

        If Not isInit Then Me.Invoke(New MethodInvoker(Sub() Me.Close()))
    End Sub

    Private Sub InitLV(lv As ListView)
        ListViewHelper.EnableDoubleBuffer(lv)

        Dim item As ListViewItem = Nothing
        Select Case lv.Name
            Case ListViewCode.Name
                item = lv.Items.Add("FFFF:FFFF".Replace("F", " "))
                With item
                    .SubItems.Add("FF FF FF FF FF FF".Replace("F", " "))
                    .SubItems.Add("FFFFFF".Replace("F", " "))
                    .SubItems.Add("FFFFFFFFFFFFFFFFFFFF".Replace("F", " "))
                End With
            Case lvStack.Name
                item = lv.Items.Add("FFFF FFFF".Replace("F", " "))
                item.SubItems.Add("FFFF".Replace("F", " "))
        End Select

        lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)
        item.Remove()
    End Sub

    Private Sub AutoSizeLastColumn(lv As ListView)
        Dim w As Integer = lv.ClientSize.Width
        Select Case lv.BorderStyle
            Case BorderStyle.Fixed3D : w -= 4
            Case BorderStyle.FixedSingle : w -= (2 + 16)
        End Select
        For i As Integer = 0 To lv.Columns.Count - 1
            w -= lv.Columns(i).Width
            If w < 1 Then Exit For
        Next

        lv.Columns(lv.Columns.Count - 1).Width += w
    End Sub

    Private Function GetBytesString(b() As Byte) As String
        Dim r As String = ""
        If b IsNot Nothing Then
            For i As Integer = 0 To b.Length - 1
                If CheckBoxBytesOrChars.Checked Then
                    r += Chr(b(i)) + " "
                Else
                    r += b(i).ToHex("") + " "
                End If
            Next
        End If
        Return r.Trim()
    End Function

    Private Sub OffsetHistoryPointer(value As Integer)
        If (historyPointer + value > 0) AndAlso (historyPointer + value < history.Length - 1) Then
            historyPointer += value

            history(historyPointer).Restore()

            RefreshCodeListing()
        End If
    End Sub

    Private Sub OffsetHistoryLoopSub()
        Do
            ohpWaiter.WaitOne(30)
            If abortThreads Then Exit Sub

            If offsetHistoryDirection <> 0 Then
                Me.Invoke(New MethodInvoker(Sub() OffsetHistoryPointer(offsetHistoryDirection)))
            End If
        Loop
    End Sub

    Private Sub SetupControls(container As Control)
        For Each c As Control In container.Controls
            If TypeOf c Is TextBox Then
                Dim tb = CType(c, TextBox)
                If tb.Name <> txtMem.Name AndAlso tb.Name <> TextBoxSearch.Name Then
                    AddHandler tb.MouseEnter, Sub() ttValueInfo.SetToolTip(tb, TextBoxValueToHuman(tb))
                    AddHandler tb.KeyUp, Sub() SetItemValue(tb)
                End If
            ElseIf c.Controls.Count > 0 Then
                SetupControls(c)
            End If
        Next
    End Sub

    Public Sub SetupCheckBoxes()
        AddHandler chkCF.CheckedChanged, Sub() mEmulator.Flags.CF = If(chkCF.Checked, 1, 0)
        AddHandler chkZF.CheckedChanged, Sub() mEmulator.Flags.ZF = If(chkZF.Checked, 1, 0)
        AddHandler chkSF.CheckedChanged, Sub() mEmulator.Flags.SF = If(chkSF.Checked, 1, 0)
        AddHandler chkOF.CheckedChanged, Sub() mEmulator.Flags.OF = If(chkOF.Checked, 1, 0)
        AddHandler chkPF.CheckedChanged, Sub() mEmulator.Flags.PF = If(chkPF.Checked, 1, 0)
        AddHandler chkAF.CheckedChanged, Sub() mEmulator.Flags.AF = If(chkAF.Checked, 1, 0)
        AddHandler chkIF.CheckedChanged, Sub() mEmulator.Flags.IF = If(chkIF.Checked, 1, 0)
        AddHandler chkDF.CheckedChanged, Sub() mEmulator.Flags.DF = If(chkDF.Checked, 1, 0)
        AddHandler chkTF.CheckedChanged, Sub() mEmulator.Flags.TF = If(chkTF.Checked, 1, 0)
    End Sub

    Private Function TextBoxValueToHuman(tb As TextBox) As String
        Dim value As Binary = Binary.From(EvaluateExpression(tb.Text).Value) And &HFFFF

        Return String.Format("{1:N0}d{0}{2}h{0}{3}b", Environment.NewLine,
                             value.ToLong(),
                             value.ToHex(),
                             value.ToString())
    End Function

    Private Sub SetItemValue(tb As TextBox)
        Dim evalRes As EvaluateResult = EvaluateExpression(tb.Text)

        Select Case tb.Name
            Case txtBreakCS.Name : breakCS = evalRes.Value
            Case txtBreakIP.Name : breakIP = evalRes.Value

            Case txtMemSeg.Name : UpdateMemory()
            Case txtMemOff.Name : UpdateMemory()

            Case Else
                mEmulator.Registers.Val(StringToRegister(tb.Name.Substring(3, 2))) = evalRes.Value
        End Select

        If evalRes.IsValid Then
            tb.BackColor = Color.FromKnownColor(KnownColor.Window)
        Else
            tb.BackColor = Color.Red
        End If
    End Sub

    Private Sub ButtonSearch_Click(sender As Object, e As EventArgs) Handles ButtonSearch.Click
        Dim str As String = TextBoxSearch.Text.ToLower()
        Dim tmp As String = ""
        Dim buffer(str.Length - 1) As Byte
        Dim found As Boolean

        ButtonSearch.Enabled = False
        TextBoxSearch.Enabled = False

        Dim startIndex As Integer = x8086.SegOffToAbs(EvaluateExpression(txtMemSeg.Text).Value, EvaluateExpression(txtMemOff.Text).Value)
        Dim endIndex As Integer = x8086.MemSize - 1 - str.Length

        If startIndex <> 0 Then startIndex += 1

        Do
            If CheckBoxTextVideoMemory.Checked Then
                Dim j As Integer
                For i As Integer = startIndex To endIndex
                    found = True

                    For j = 0 To str.Length - 1 Step 2
                        If i + j * 2 >= x8086.MemSize Then
                            found = False
                            Exit For
                        End If

                        If mEmulator.Memory(i + j * 2) <> Asc(str(j)) Then
                            found = False
                            Exit For
                        End If
                    Next

                    If found Then
                        txtMemSeg.Text = x8086.AbsToSeg(i).ToHex(x8086.DataSize.Word, "")
                        txtMemOff.Text = x8086.AbsoluteToOff(i).ToHex(x8086.DataSize.Word, "")

                        Exit Do
                    End If
                Next
            Else
                For i As Integer = startIndex To endIndex
                    Array.Copy(mEmulator.Memory, i, buffer, 0, str.Length)
                    If ASCIIEncoding.ASCII.GetString(buffer).ToLower() = str Then
                        txtMemSeg.Text = x8086.AbsToSeg(i).ToHex(x8086.DataSize.Word, "")
                        txtMemOff.Text = x8086.AbsoluteToOff(i).ToHex(x8086.DataSize.Word, "")

                        found = True
                        Exit Do
                    End If
                Next
            End If

            If startIndex = 0 Then
                Exit Do
            Else
                endIndex = startIndex
                startIndex = 0
            End If
        Loop

        If found Then
            TextBoxSearch.BackColor = Color.LightGreen
        Else
            TextBoxSearch.BackColor = Color.LightSalmon
        End If

        UpdateMemory()

        ButtonSearch.Enabled = True
        TextBoxSearch.Enabled = True
    End Sub

    Private Sub ButtonMemBack_Click(sender As Object, e As EventArgs) Handles ButtonMemBack.Click
        Dim address As Integer = x8086.SegOffToAbs(EvaluateExpression(txtMemSeg.Text).Value, EvaluateExpression(txtMemOff.Text).Value)
        address -= 256
        txtMemSeg.Text = x8086.AbsToSeg(address).ToHex(x8086.DataSize.Word)
        txtMemOff.Text = x8086.AbsoluteToOff(address).ToHex(x8086.DataSize.Word)

        UpdateMemory()
    End Sub

    Private Sub ButtonMemForward_Click(sender As Object, e As EventArgs) Handles ButtonMemForward.Click
        Dim address As Integer = x8086.SegOffToAbs(EvaluateExpression(txtMemSeg.Text).Value, EvaluateExpression(txtMemOff.Text).Value)
        address += 256
        txtMemSeg.Text = x8086.AbsToSeg(address).ToHex(x8086.DataSize.Word)
        txtMemOff.Text = x8086.AbsoluteToOff(address).ToHex(x8086.DataSize.Word)

        UpdateMemory()
    End Sub

    Private Sub TextBoxSearch_TextChanged(sender As Object, e As EventArgs) Handles TextBoxSearch.TextChanged
        TextBoxSearch.BackColor = Color.FromKnownColor(KnownColor.Window)
    End Sub

    Private Sub CheckBoxBytesOrChars_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxBytesOrChars.CheckedChanged
        ListViewCode.Items.Clear()
        UpdateUI()
    End Sub
End Class