Partial Public Class X8086
    Public Shared Property LogToConsole As Boolean

    Private mMPIs As Double
    Private mEmulateINT13 As Boolean = True
    Private mVic20 As Boolean

    Private mVideoAdapter As VideoAdapter
    Private mKeyboard As KeyboardAdapter
    Private mMouse As MouseAdapter
    Private mFloppyController As FloppyControllerAdapter
    Private mAdapters As Adapters = New Adapters(Me)
    Private mPorts As IOPorts = New IOPorts(Me)
    Private mEnableExceptions As Boolean

    Private mDebugMode As Boolean
    Private mIsPaused As Boolean

    Public ReadOnly Property V20 As Boolean
        Get
            Return mVic20
        End Get
    End Property

    Public ReadOnly Property MIPs As Double
        Get
            Return mMPIs
        End Get
    End Property

    Public Property SimulationMultiplier As Double
        Get
            Return mSimulationMultiplier
        End Get
        Set(ByVal value As Double)
            mSimulationMultiplier = value
            SetSynchronization()
        End Set
    End Property

    Public Property Clock As Double
        Get
            Return mCyclesPerSecond
        End Get
        Set(ByVal value As Double)
            mCyclesPerSecond = value
            SetSynchronization()
        End Set
    End Property

    Public ReadOnly Property REPELoopMode As REPLoopModes
        Get
            Return mRepeLoopMode
        End Get
    End Property

    Public ReadOnly Property Adapters As Adapters
        Get
            Return mAdapters
        End Get
    End Property

    Public ReadOnly Property Keyboard As KeyboardAdapter
        Get
            Return mKeyboard
        End Get
    End Property

    Public ReadOnly Property VideoAdapter As VideoAdapter
        Get
            Return mVideoAdapter
        End Get
    End Property

    Public ReadOnly Property FloppyContoller As FloppyControllerAdapter
        Get
            Return mFloppyController
        End Get
    End Property

    Public ReadOnly Property Ports As IOPorts
        Get
            Return mPorts
        End Get
    End Property

    Public Property EnableExceptions As Boolean
        Get
            Return mEnableExceptions
        End Get
        Set(value As Boolean)
            mEnableExceptions = value
        End Set
    End Property

    Public Property DebugMode As Boolean
        Get
            Return mDebugMode
        End Get
        Set(value As Boolean)
            If mDebugMode And Not value Then
                mDebugMode = False
                debugWaiter.Set()
            Else
                mDebugMode = value
                mDoReSchedule = True
                RaiseEvent DebugModeChanged(Me, New EventArgs())
            End If
        End Set
    End Property

    Public ReadOnly Property IsExecuting As Boolean
        Get
            Return mIsExecuting
        End Get
    End Property

    Private mIsHalted As Boolean
    Public ReadOnly Property IsHalted As Boolean
        Get
            Return mIsHalted
        End Get
    End Property

    Public ReadOnly Property IsPaused As Boolean
        Get
            Return mIsPaused
        End Get
    End Property

    Public ReadOnly Property Model As X8086.Models
        Get
            Return mModel
        End Get
    End Property

    Public ReadOnly Property Mouse As MouseAdapter
        Get
            Return mMouse
        End Get
    End Property

    Public ReadOnly Property EmulateINT13 As Boolean
        Get
            Return mEmulateINT13
        End Get
    End Property

    Private Function GetCpuSpeed() As UInt32
#If Win32 Then
        Using managementObject As New Management.ManagementObject("Win32_Processor.DeviceID='CPU0'")
            Return managementObject("CurrentClockSpeed")
        End Using
#Else
        Return 1000
#End If
    End Function
End Class