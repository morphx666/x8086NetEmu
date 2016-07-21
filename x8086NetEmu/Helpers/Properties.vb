Partial Public Class x8086
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

    Public ReadOnly Property VideoAdapter As CGAAdapter
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
            mDebugMode = value

            mDoReSchedule = True
            If Not mDebugMode Then debugWaiter.Set()
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

    Public ReadOnly Property Model As x8086.Models
        Get
            Return mModel
        End Get
    End Property

    Public ReadOnly Property Mouse As MouseAdapter
        Get
            Return mMouse
        End Get
    End Property


    Public Property EmulateINT13 As Boolean
        Get
            Return mEmulateINT13
        End Get
        Set(value As Boolean)
            mEmulateINT13 = value
        End Set
    End Property
End Class
