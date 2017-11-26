Public Class IOPorts
    Implements IList(Of IOPortHandler)

    Private emulator As X8086
    Private list As List(Of IOPortHandler)

    Public Sub New(emulator As X8086)
        Me.emulator = emulator
        list = New List(Of IOPortHandler)
    End Sub

    Public Sub Add(item As IOPortHandler) Implements System.Collections.Generic.ICollection(Of IOPortHandler).Add
        list.Add(item)
    End Sub

    Public Sub Clear() Implements System.Collections.Generic.ICollection(Of IOPortHandler).Clear
        list.Clear()
    End Sub

    Public Function Contains(item As IOPortHandler) As Boolean Implements System.Collections.Generic.ICollection(Of IOPortHandler).Contains
        Return list.Contains(item)
    End Function

    Public Sub CopyTo(array() As IOPortHandler, arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of IOPortHandler).CopyTo

    End Sub

    Public ReadOnly Property Count As Integer Implements System.Collections.Generic.ICollection(Of IOPortHandler).Count
        Get
            Return list.Count
        End Get
    End Property

    Public ReadOnly Property IsReadOnly As Boolean Implements System.Collections.Generic.ICollection(Of IOPortHandler).IsReadOnly
        Get
            Return False
        End Get
    End Property

    Public Function Remove(item As IOPortHandler) As Boolean Implements System.Collections.Generic.ICollection(Of IOPortHandler).Remove
        Return list.Remove(item)
    End Function

    Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of IOPortHandler) Implements System.Collections.Generic.IEnumerable(Of IOPortHandler).GetEnumerator
        Return list.GetEnumerator()
    End Function

    Public Function IndexOf(item As IOPortHandler) As Integer Implements System.Collections.Generic.IList(Of IOPortHandler).IndexOf
        Return list.IndexOf(item)
    End Function

    Public Sub Insert(index As Integer, item As IOPortHandler) Implements System.Collections.Generic.IList(Of IOPortHandler).Insert
        list.Insert(index, item)
    End Sub

    Default Public Property Item(index As Integer) As IOPortHandler Implements System.Collections.Generic.IList(Of IOPortHandler).Item
        Get
            Return list.Item(index)
        End Get
        Protected Set(value As IOPortHandler)
            list.Item(index) = value
        End Set
    End Property

    Public Sub RemoveAt(index As Integer) Implements System.Collections.Generic.IList(Of IOPortHandler).RemoveAt
        list.RemoveAt(index)
    End Sub

    Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return list.GetEnumerator()
    End Function
End Class
