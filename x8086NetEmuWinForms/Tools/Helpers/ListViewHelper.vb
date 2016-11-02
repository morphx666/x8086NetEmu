Imports System.Runtime.InteropServices

Public Enum ListViewExtendedStyles
    ' <summary>
    ' LVS_EX_GRIDLINES
    ' </summary>
    GridLines = &H1
    ' <summary>
    ' LVS_EX_SUBITEMIMAGES
    ' </summary>
    SubItemImages = &H2
    ' <summary>
    ' LVS_EX_CHECKBOXES
    ' </summary>
    CheckBoxes = &H4
    ' <summary>
    ' LVS_EX_TRACKSELECT
    ' </summary>
    TrackSelect = &H8
    ' <summary>
    ' LVS_EX_HEADERDRAGDROP
    ' </summary>
    HeaderDragDrop = &H10
    ' <summary>
    ' LVS_EX_FULLROWSELECT
    ' </summary>
    FullRowSelect = &H20
    ' <summary>
    ' LVS_EX_ONECLICKACTIVATE
    ' </summary>
    OneClickActivate = &H40
    ' <summary>
    ' LVS_EX_TWOCLICKACTIVATE
    ' </summary>
    TwoClickActivate = &H80
    ' <summary>
    ' LVS_EX_FLATSB
    ' </summary>
    FlatsB = &H100
    ' <summary>
    ' LVS_EX_REGIONAL
    ' </summary>
    Regional = &H200
    ' <summary>
    ' LVS_EX_INFOTIP
    ' </summary>
    InfoTip = &H400
    ' <summary>
    ' LVS_EX_UNDERLINEHOT
    ' </summary>
    UnderlineHot = &H800
    ' <summary>
    ' LVS_EX_UNDERLINECOLD
    ' </summary>
    UnderlineCold = &H1000
    ' <summary>
    ' LVS_EX_MULTIWORKAREAS
    ' </summary>
    MultilWorkAreas = &H2000
    ' <summary>
    ' LVS_EX_LABELTIP
    ' </summary>
    LabelTip = &H4000
    ' <summary>
    ' LVS_EX_BORDERSELECT
    ' </summary>
    BorderSelect = &H8000
    ' <summary>
    ' LVS_EX_DOUBLEBUFFER
    ' </summary>
    DoubleBuffer = &H10000
    ' <summary>
    ' LVS_EX_HIDELABELS
    ' </summary>
    HideLabels = &H20000
    ' <summary>
    ' LVS_EX_SINGLEROW
    ' </summary>
    SingleRow = &H40000
    ' <summary>
    ' LVS_EX_SNAPTOGRID
    ' </summary>
    SnapToGrid = &H80000
    ' <summary>
    ' LVS_EX_SIMPLESELECT
    ' </summary>
    SimpleSelect = &H100000
End Enum

Public Enum ListViewMessages
    First = &H1000
    SetExtendedStyle = (First + 54)
    GetExtendedStyle = (First + 55)
End Enum

' <summary>
' Contains helper methods to change extended styles on ListView, including enabling double buffering.
' Based on Giovanni Montrone's article on <see cref="http://www.codeproject.com/KB/list/listviewxp.aspx"/>
' </summary>
Public Class ListViewHelper
    Private Sub New()
    End Sub

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function SendMessage(handle As IntPtr, messg As Integer, wparam As Integer, lparam As Integer) As Integer
    End Function

    Public Shared Sub SetExtendedStyle(control As Control, exStyle As ListViewExtendedStyles)
        Dim styles As ListViewExtendedStyles
        styles = CType(SendMessage(control.Handle, CInt(ListViewMessages.GetExtendedStyle), 0, 0), ListViewExtendedStyles)
        styles = styles Or exStyle
        SendMessage(control.Handle, CInt(ListViewMessages.SetExtendedStyle), 0, CInt(styles))
    End Sub

    Public Shared Sub EnableDoubleBuffer(control As Control)
        Dim styles As ListViewExtendedStyles
        ' read current style
        styles = CType(SendMessage(control.Handle, CInt(ListViewMessages.GetExtendedStyle), 0, 0), ListViewExtendedStyles)
        ' enable double buffer and border select
        styles = styles Or ListViewExtendedStyles.DoubleBuffer Or ListViewExtendedStyles.BorderSelect
        ' write new style
        SendMessage(control.Handle, CInt(ListViewMessages.SetExtendedStyle), 0, CInt(styles))
    End Sub

    Public Shared Sub DisableDoubleBuffer(control As Control)
        Dim styles As ListViewExtendedStyles
        ' read current style
        styles = CType(SendMessage(control.Handle, CInt(ListViewMessages.GetExtendedStyle), 0, 0), ListViewExtendedStyles)
        ' disable double buffer and border select
        styles -= styles And ListViewExtendedStyles.DoubleBuffer
        styles -= styles And ListViewExtendedStyles.BorderSelect
        ' write new style
        SendMessage(control.Handle, CInt(ListViewMessages.SetExtendedStyle), 0, CInt(styles))
    End Sub

    <DllImport("user32.dll")>
    Private Shared Function GetScrollInfo(ByVal hwnd As IntPtr, ByVal fnBar As Integer, ByRef lpsi As SCROLLINFO) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetScrollInfo(ByVal hwnd As IntPtr, ByVal fnBar As Integer, <[In]()> ByRef lpsi As SCROLLINFO, ByVal fRedraw As Boolean) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Private Shared Function PostMessage(ByVal hWnd As IntPtr, ByVal Msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Boolean
    End Function

    Private Structure SCROLLINFO
        Public cbSize As Integer
        Public fMask As Integer
        Public nMin As Integer
        Public nMax As Integer
        Public nPage As Integer
        Public nPos As Integer
        Public nTrackPos As Integer
    End Structure

    Private Enum ScrollBarDirection
        SB_HORZ = 0
        SB_VERT = 1
        SB_CTL = 2
        SB_BOTH = 3
    End Enum

    Private Enum ScrollInfoMask
        SIF_RANGE = &H1
        SIF_PAGE = &H2
        SIF_POS = &H4
        SIF_DISABLENOSCROLL = &H8
        SIF_TRACKPOS = &H10
        SIF_ALL = SIF_RANGE + SIF_PAGE + SIF_POS + SIF_TRACKPOS
    End Enum

    Private Const SB_THUMBTRACK As Integer = 5
    Private Const WM_VSCROLL As Integer = &H115
    Private Const WM_HSCROLL As Integer = &H114
    Private Const LVM_FIRST = &H1000
    Private Const LVM_SCROLL = (LVM_FIRST + 20)

    Private Declare Function SetScrollPos Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal nBar As Integer, ByVal nPos As Integer, ByVal bRedraw As Boolean) As Integer

    Public Shared Property VerticalScroll(control As Control) As Integer
        Get
            Dim si As New SCROLLINFO()
            si.cbSize = CUInt(Marshal.SizeOf(si))
            si.fMask = CUInt(ScrollInfoMask.SIF_ALL)
            GetScrollInfo(control.Handle, CInt(ScrollBarDirection.SB_VERT), si)
            Return si.nPos
        End Get
        Set(ByVal value As Integer)
            Dim si As New SCROLLINFO()
            si.cbSize = CUInt(Marshal.SizeOf(si))
            si.fMask = CUInt(ScrollInfoMask.SIF_ALL)
            GetScrollInfo(control.Handle, CInt(ScrollBarDirection.SB_VERT), si)
            If value > si.nMax Then value = si.nMax

            'Dim ptrWparam = New IntPtr(SB_THUMBTRACK + &H10000 * value)
            'SetScrollPos(control.Handle, Orientation.Vertical, value, True)
            'PostMessage(control.Handle, WM_VSCROLL, ptrWparam, IntPtr.Zero)
            SendMessage(control.Handle, LVM_SCROLL, 0, value)
        End Set
    End Property
End Class
