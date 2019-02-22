Partial Public Class X8086
    Public Class EmulatorErrorEventArgs
        Inherits EventArgs

        Private mMessage As String

        Public Sub New(msg As String)
            mMessage = msg
        End Sub

        Public ReadOnly Property Message As String
            Get
                Return mMessage
            End Get
        End Property
    End Class

    Private Sub OpCodeNotImplemented(Optional comment As String = "")
        Dim originalOpCodeSize As Integer = opCodeSize
        ThrowException(String.Format("OpCode '{0}' at {1} Not Implemented{2}", Decode(Me, True).Mnemonic?.Replace("h:", ""),
                                                                               mRegisters.PointerAddressToString().Replace("h", ""),
                                                                               If(comment = "", "", ": " + comment)))
        opCodeSize = originalOpCodeSize
        If mVic20 Then HandleInterrupt(6, False)
    End Sub

    Private Sub InterruptNotImplemented(intNum As Integer)
        ThrowException("Interrupt 0x" + intNum.ToString("X2") + " not implemented")
    End Sub

    Private Sub InterruptModeNotImplemented(intNum As Integer, mode As Integer)
        ThrowException("Interrupt 0x" + intNum.ToString("X2") + " mode 0x" + mode.ToString("X2") + " not implemented")
    End Sub

    Private Sub InterruptSubModeNotImplemented(intNum As Integer, mode As Integer, subMode As Integer)
        ThrowException("Interrupt 0x" + intNum.ToString("X2") + " mode 0x" + mode.ToString("X2") + "/0x" + subMode.ToString("X2") + " not implemented")
    End Sub

    Private Sub KeyboardAdapterNotFound()
        ThrowException("Keyboard Adapter Not Found")
    End Sub

    Private Sub DiskAdapterNotFound()
        ThrowException("Disk Adapter Not Found")
    End Sub

    Private Sub SystemReboot()
        ThrowException("System Reboot")
    End Sub

    Private Sub DivisionByZero()
        ThrowException("Division By Zero")
    End Sub


    Private Sub SystemHalted()
        mIsHalted = True
        ThrowException("System Halted")

        RaiseEvent EmulationHalted()

#If DEBUG Then
        RaiseEvent InstructionDecoded()
#End If
    End Sub

    Private Sub NoIOPort(port As Integer)
        X8086.Notify("No IO port response from {0} called at {1}:{2}", NotificationReasons.Warn,
                        port.ToString("X4"),
                        mRegisters.CS.ToString("X4"),
                        mRegisters.IP.ToString("X4"))
    End Sub

    Public Sub RaiseException(message As String)
        ThrowException(message)
    End Sub

    Private Sub ThrowException(message As String)
        If mEnableExceptions Then
            Throw New Exception(message)
        Else
            X8086.Notify(message, NotificationReasons.Err)
            RaiseEvent Error(Me, New EmulatorErrorEventArgs(message))
        End If
    End Sub

    Public Enum NotificationReasons
        Info
        Warn
        Err
        Fck
        Dbg
    End Enum

    Public Shared Sub Notify(message As String, reason As NotificationReasons, ParamArray arg() As Object)
        Dim formattedMessage = reason.ToString().PadRight(4) + " " + String.Format(message, arg)

        If LogToConsole Then
            Console.WriteLine(formattedMessage)
#If DEBUG Then
            If reason = NotificationReasons.Dbg Then Debug.WriteLine(formattedMessage)
#End If
        End If

        RaiseEvent Output(message, reason, arg)
    End Sub
End Class
