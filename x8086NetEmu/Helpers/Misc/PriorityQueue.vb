Public Class PriorityQueue
    Private nHeap As Integer = 0
    Private heapObj(16 - 1) As Object
    Private heapPri(16 - 1) As Long

    Public Sub New()
    End Sub

    Public Sub Clear()
        nHeap = 0
        ReDim heapObj(16 - 1)
        ReDim heapPri(16 - 1)
    End Sub

    Public Sub Add(obj As Object, priority As Long)
        nHeap += 1
        If nHeap >= heapObj.Length Then
            Dim oldHeapObj() As Object = heapObj
            Dim oldHeapPri() As Long = heapPri
            ReDim heapObj(2 * nHeap - 1)
            ReDim heapPri(2 * nHeap - 1)
            Array.Copy(oldHeapObj, 0, heapObj, 0, nHeap)
            Array.Copy(oldHeapPri, 0, heapPri, 0, nHeap)
        End If

        heapPri(0) = Long.MinValue ' element 0 is a sentinel
        Dim k As Integer = nHeap
        While heapPri(k \ 2) > priority
            heapObj(k) = heapObj(k \ 2)
            heapPri(k) = heapPri(k \ 2)
            k = k \ 2
        End While

        heapObj(k) = obj
        heapPri(k) = priority
    End Sub

    Public Function MinPriority() As Long
        Return If(nHeap > 0, heapPri(1), Long.MaxValue)
    End Function

    Public Function RemoveFirst() As Object
        If nHeap = 0 Then Return Nothing

        Dim obj As Object = heapObj(1)

        Dim vo As Object = heapObj(nHeap)
        Dim vp As Long = heapPri(nHeap)
        nHeap -= 1

        Dim k As Integer = 1
        Dim j As Integer
        While k <= nHeap \ 2
            j = 2 * k
            If j < nHeap AndAlso heapPri(j) > heapPri(j + 1) Then j += 1
            If vp <= heapPri(j) Then Exit While

            heapObj(k) = heapObj(j)
            heapPri(k) = heapPri(j)
            k = j
        End While
        heapObj(k) = vo
        heapPri(k) = vp

        Return obj
    End Function

    Public Sub Remove(obj As Object)
        Dim k As Integer = 1
        While k <= nHeap AndAlso (Not heapObj(k) Is obj)
            k += 1
        End While

        If k <= nHeap Then
            Dim vo As Object = heapObj(nHeap)
            Dim vp As Long = heapPri(nHeap)
            nHeap -= 1

            Dim j As Integer
            While k <= nHeap \ 2
                j = 2 * k
                If j < nHeap AndAlso heapPri(j) > heapPri(j + 1) Then j += 1
                If vp <= heapPri(j) Then Exit While

                heapObj(k) = heapObj(j)
                heapPri(k) = heapPri(j)
                k = j
            End While
            heapObj(k) = vo
            heapPri(k) = vp
        End If
    End Sub

    Public ReadOnly Property Size As Integer
        Get
            Return nHeap
        End Get
    End Property

    Public ReadOnly Property IsEmpty As Boolean
        Get
            Return nHeap = 0
        End Get
    End Property
End Class
