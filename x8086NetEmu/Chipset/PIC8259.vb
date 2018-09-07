Public Class PIC8259
    Inherits IOPortHandler

    Private Enum States
        Ready = 0
        ICW1 = 1
        ICW2 = 2
        ICW3 = 3
        ICW4 = 4
    End Enum

    Private state As States
    Private expectICW3 As Boolean
    Private expectICW4 As Boolean

    Private slave() As PIC8259
    Private master As PIC8259
    Private masterIrq As Integer

    Private levelTriggered As Boolean
    Private autoEOI As Boolean
    Private autoRotate As Boolean
    Private baseVector As Integer
    Private specialMask As Boolean
    Private specialNest As Boolean
    Private pollMode As Boolean
    Private readISR As Boolean
    Private lowPrio As Integer
    Private slaveInput As Integer
    Private cascadeId As Integer
    Private rIMR As Integer
    Private rIRR As Integer
    Private rISR As Integer

    Public Class IRQLine
        Inherits InterruptRequest

        Private mPic As PIC8259
        Private mIrq As Integer

        Public Sub New(pic As PIC8259, irq As Integer)
            mPic = pic
            mIrq = irq
        End Sub

        Public Overrides Sub Raise(enable As Boolean)
            mPic.RaiseIrq(mIrq, enable)
        End Sub
    End Class

    Public Sub New(cpu As X8086, Optional master As PIC8259 = Nothing)
        ReDim slave(8 - 1)

        If master Is Nothing Then
            For i As Integer = &H20 To &H2F
                ValidPortAddress.Add(i)
            Next

            'cascadeId = 0
            'slave(cascadeId) = New PIC8259(cpu, Me)
            'slave(cascadeId).SetMaster(Me, 2)
        Else
            For i As Integer = &H30 To &H3F
                ValidPortAddress.Add(i)
            Next
        End If

        state = States.ICW1
    End Sub

    Public Overrides Function GetPendingInterrupt() As Integer
        If state <> States.Ready Then Return -1

        ' Determine set of pending interrupt requests
        Dim reqmask As Integer = rIRR And (Not rIMR)
        If specialNest Then
            reqmask = reqmask And ((Not rISR) Or slaveInput)
        Else
            reqmask = reqmask And (Not rISR)
        End If

        ' NOTE: This appears to fix an issue with the current mouse adapter
        reqmask = reqmask And &HFF

        ' Select non-masked request with highest priority
        If reqmask = 0 Then Return -1

        Dim irq As Integer = (lowPrio + 1) And 7
        While (reqmask And (1 << irq)) = 0
            If Not specialMask AndAlso ((rISR And (1 << irq)) <> 0) Then Return -1 ' ISR bit blocks all lower-priority requests
            irq = (irq + 1) And 7
        End While

        Dim irqbit As Integer = (1 << irq)

        ' Update controller state
        If Not autoEOI Then rISR = rISR Or irqbit
        If Not levelTriggered Then rIRR = rIRR And (Not irqbit)
        If autoEOI AndAlso autoRotate Then lowPrio = irq
        If master IsNot Nothing Then UpdateSlaveOutput()

        ' Return vector number or pass down to slave controller
        If (slaveInput And irqbit) <> 0 AndAlso slave(irq) IsNot Nothing Then
            Return slave(irq).GetPendingInterrupt()
        Else
            Return (baseVector + irq) And &HFF
        End If
    End Function

    Public Function GetIrqLine(i As Integer) As IRQLine
        Return New IRQLine(Me, i)
    End Function

    Public Sub RaiseIrq(irq As Integer, enable As Boolean)
        If enable Then
            rIRR = rIRR Or (1 << irq)
        Else
            rIRR = rIRR And (Not (1 << irq))
        End If
        If master IsNot Nothing Then UpdateSlaveOutput()
    End Sub

    Public Overrides Function [In](port As UInt32) As UInt32
        If (port And 1) = 0 Then
            ' A0 == 0
            If pollMode Then
                Dim a = GetPendingInterrupt()
                Return If(a = -1, 0, &H80 Or a)
            End If
            Return If(readISR, rISR, rIRR)
        Else
            ' A0 == 1
            Return rIMR
        End If
    End Function

    Public Overrides Sub Out(port As UInt32, value As UInt32)
        If (port And 1) = 0 Then
            ' A0 = 0
            If (value And &H10) <> 0 Then
                DoICW1(value)
            ElseIf (value And &H8) = 0 Then
                DoOCW2(value)
            Else
                DoOCW3(value)
            End If
        Else
            ' A0 == 1
            Select Case state
                Case States.ICW2 : DoICW2(value)
                Case States.ICW3 : DoICW3(value)
                Case States.ICW4 : DoICW4(value)
                Case Else : DoOCW1(value)
            End Select
        End If
    End Sub

    Private Sub UpdateSlaveOutput()
        Dim reqmask As Integer = rIRR And (Not rIMR)
        If Not specialMask Then reqmask = reqmask And (Not rISR)
        If master IsNot Nothing Then master.RaiseIrq(masterIrq, (reqmask <> 0))
    End Sub

    Public Sub SetMaster(pic As PIC8259, irq As Integer)
        If master IsNot Nothing Then master.slave(cascadeId) = Nothing
        master = pic
        masterIrq = irq
        If master IsNot Nothing Then master.slave(cascadeId) = Me
    End Sub

    Private Sub DoICW1(v As Integer)
        state = States.ICW2
        rIMR = 0
        rISR = 0
        specialMask = False
        specialNest = False
        autoEOI = False
        autoRotate = False
        pollMode = False
        readISR = False
        lowPrio = 7
        slaveInput = 0
        If master IsNot Nothing Then master.slave(cascadeId) = Nothing
        cascadeId = 7
        If master IsNot Nothing Then master.slave(cascadeId) = Me
        levelTriggered = (v And &H8) <> 0
        expectICW3 = (v And &H2) = 0
        expectICW4 = (v And &H1) <> 0
        If master IsNot Nothing Then UpdateSlaveOutput()
    End Sub

    Private Sub DoICW2(v As Integer)
        baseVector = v And &HF8
        state = If(expectICW3, If(expectICW4, States.ICW4, States.Ready), States.ICW3)
    End Sub

    Private Sub DoICW3(v As Integer)
        slaveInput = v
        If master IsNot Nothing Then master.slave(cascadeId) = Nothing
        cascadeId = v And &H7
        If master IsNot Nothing Then master.slave(cascadeId) = Me
        state = If(expectICW4, States.ICW4, States.Ready)
    End Sub

    Private Sub DoICW4(v As Integer)
        specialNest = (v And &H10) <> 0
        autoEOI = (v And &H2) <> 0
        state = States.Ready
    End Sub

    Private Sub DoOCW1(v As Integer)
        rIMR = v
        If master IsNot Nothing Then UpdateSlaveOutput()
    End Sub

    Private Sub DoOCW2(v As Integer)
        Dim irq As Integer = v And &H7
        Dim rotate As Boolean = (v And &H80) <> 0
        Dim specific As Boolean = (v And &H40) <> 0
        Dim eoi As Boolean = (v And &H20) <> 0

        ' Resolve non-specific EOI
        If Not specific Then
            Dim m As Integer = If(specialMask, rISR And (Not rIMR), rISR)
            Dim i As Integer = lowPrio
            Do
                i = (i + 1) And 7
                If (m And (1 << i)) <> 0 Then
                    irq = i
                    Exit Do
                End If
            Loop While i <> lowPrio
        End If

        If eoi Then
            rISR = rISR And (Not (1 << irq))
            If master IsNot Nothing Then UpdateSlaveOutput()
        End If

        If Not eoi AndAlso Not specific Then
            autoRotate = rotate
        ElseIf rotate Then
            lowPrio = irq
        End If
    End Sub

    Private Sub DoOCW3(v As Integer)
        If (v And &H40) <> 0 Then
            specialMask = (v And &H20) <> 0
            If master IsNot Nothing Then UpdateSlaveOutput()
        End If

        pollMode = (v And &H4) <> 0
        If (v And &H2) <> 0 Then readISR = ((v And &H1) <> 0)
    End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return "8259"
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "8259 Programmable Interrupt Controller"
        End Get
    End Property

    Public Overrides Sub Run()
    End Sub
End Class
