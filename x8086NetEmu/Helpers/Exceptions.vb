Partial Public Class x8086
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

    Private Sub OpCodeNotImplemented(opCode As Byte, Optional comment As String = "")
        Dim originalOpCodeSize As Integer = opCodeSize
        ThrowException(String.Format("OpCode '{0}' at {1} Not Implemented{2}", Decode(Me, True).Mnemonic.Replace("h:", ""),
                                                                                        mRegisters.PointerAddressToString().Replace("h", ""),
                                                                                        If(comment = "", "", ": " + comment)))
        opCodeSize = originalOpCodeSize
    End Sub

    Private Sub InterruptNotImplemented(intNum As Integer)
        ThrowException("Interrupt 0x" + intNum.ToHex(DataSize.Byte) + " not implemented")
    End Sub

    Private Sub InterruptModeNotImplemented(intNum As Integer, mode As Integer)
        ThrowException("Interrupt 0x" + intNum.ToHex(DataSize.Byte) + " mode 0x" + mode.ToHex(DataSize.Byte) + " not implemented")
    End Sub

    Private Sub InterruptSubModeNotImplemented(intNum As Integer, mode As Integer, subMode As Integer)
        ThrowException("Interrupt 0x" + intNum.ToHex(DataSize.Byte) + " mode 0x" + mode.ToHex(DataSize.Byte) + "/0x" + subMode.ToHex(DataSize.Byte) + " not implemented")
    End Sub

    Private Sub VGAAdapterNotFound()
        ThrowException("VGA Adapter Not Found")
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

    Private Sub SystemHalted()
        ThrowException("System Halted")
        mIsHalted = True

        RaiseEvent EmulationHalted()

#If DEBUG Then
        RaiseEvent InstructionDecoded()
#End If
    End Sub

    Private Sub NoIOPort(port As Integer)
        x8086.Notify("No IO port responding from {0} called at {1}:{2}", NotificationReasons.Warn,
                        port.ToHex(DataSize.Word),
                        mRegisters.CS.ToHex(DataSize.Word).TrimEnd("h"),
                        mRegisters.IP.ToHex(DataSize.Word).TrimEnd("h"))
    End Sub

    Public Sub RaiseException(message As String)
        ThrowException(message)
    End Sub

    Private Sub ThrowException(message As String)
        Debug.WriteLine(message)
        If mEnableExceptions Then
            Throw New Exception(message)
        Else
            x8086.Notify(message, NotificationReasons.Err)
            RaiseEvent Error(Me, New EmulatorErrorEventArgs(message))
        End If
    End Sub

    Public Enum NotificationReasons
        Info
        Warn
        Err
        Fck
    End Enum

    Public Shared Sub Notify(message As String, reason As NotificationReasons, ParamArray arg() As Object)
        Dim formattedMessage = reason.ToString().PadRight(4) + " " + String.Format(message, arg)

        If LogToConsole Then Console.WriteLine(formattedMessage)

        RaiseEvent Output(message, reason, arg)
    End Sub
End Class
