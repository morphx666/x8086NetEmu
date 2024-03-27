Imports ManagedBass

Public Class BufferProvider
    Private position As Long = 0
    Private buffer() As Byte = Nothing

    Private mFileProcs As FileProcedures
    Public ReadOnly Property FileProcs As FileProcedures
        Get
            Return mFileProcs
        End Get
    End Property

    Private mHandle As Integer
    Public Property Handle As Integer
        Get
            Return mHandle
        End Get
        Private Set(value As Integer)
            mHandle = value
        End Set
    End Property

    Public Sub New()
        mFileProcs = New FileProcedures With {
            .Read = New FileReadProcedure(AddressOf BassFileRead),
            .Seek = New FileSeekProcedure(AddressOf BassFileSeek),
            .Length = New FileLengthProcedure(AddressOf BassFileLength),
            .Close = New FileCloseProcedure(AddressOf BassFileClose)
        }
    End Sub

    Private Function BassFileRead(Buffer As IntPtr, Length As Integer, User As IntPtr) As Integer

    End Function

    Private Function BassFileSeek(Offset As Long, User As IntPtr) As Boolean
        Return False
    End Function

    Private Function BassFileLength(User As IntPtr) As Long
        Return Long.MaxValue
    End Function

    Private Sub BassFileClose(User As IntPtr)
        If mHandle <> 0 Then
            Bass.StreamFree(mHandle)
            mHandle = 0
        End If
    End Sub
End Class
