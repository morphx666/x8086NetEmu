Public Class IOPorts
    Implements IList(Of IOPortHandler)

    Private emulator As X8086
    Private list As List(Of IOPortHandler)

    Public Sub New(emulator As X8086)
        Me.emulator = emulator
        list = New List(Of IOPortHandler)
    End Sub

    Public Sub Add(item As IOPortHandler) Implements ICollection(Of IOPortHandler).Add
        list.Add(item)
    End Sub

    Public Sub Clear() Implements ICollection(Of IOPortHandler).Clear
        list.Clear()
    End Sub

    Public Function Contains(item As IOPortHandler) As Boolean Implements ICollection(Of IOPortHandler).Contains
        Return list.Contains(item)
    End Function

    Public Sub CopyTo(array() As IOPortHandler, arrayIndex As Integer) Implements ICollection(Of IOPortHandler).CopyTo

    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection(Of IOPortHandler).Count
        Get
            Return list.Count
        End Get
    End Property

    Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of IOPortHandler).IsReadOnly
        Get
            Return False
        End Get
    End Property

    Public Function Remove(item As IOPortHandler) As Boolean Implements ICollection(Of IOPortHandler).Remove
        Return list.Remove(item)
    End Function

    Public Function GetEnumerator() As IEnumerator(Of IOPortHandler) Implements IEnumerable(Of IOPortHandler).GetEnumerator
        Return list.GetEnumerator()
    End Function

    Public Function IndexOf(item As IOPortHandler) As Integer Implements IList(Of IOPortHandler).IndexOf
        Return list.IndexOf(item)
    End Function

    Public Sub Insert(index As Integer, item As IOPortHandler) Implements IList(Of IOPortHandler).Insert
        list.Insert(index, item)
    End Sub

    Default Public Property Item(index As Integer) As IOPortHandler Implements IList(Of IOPortHandler).Item
        Get
            Return list.Item(index)
        End Get
        Protected Set(value As IOPortHandler)
            list.Item(index) = value
        End Set
    End Property

    Public Sub RemoveAt(index As Integer) Implements IList(Of IOPortHandler).RemoveAt
        list.RemoveAt(index)
    End Sub

    Public Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Return list.GetEnumerator()
    End Function
End Class
