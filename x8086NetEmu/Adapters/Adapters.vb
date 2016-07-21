Public Class Adapters
    Implements IList(Of Adapter)

    Private emulator As x8086
    Private list As List(Of Adapter)

    Public Sub New(emulator As x8086)
        Me.emulator = emulator
        list = New List(Of Adapter)
    End Sub

    Public Sub Add(adapter As Adapter) Implements System.Collections.Generic.ICollection(Of Adapter).Add
        emulator.SetUpAdapter(adapter)
        list.Add(adapter)
    End Sub

    Public Sub Clear() Implements System.Collections.Generic.ICollection(Of Adapter).Clear
        list.Clear()
    End Sub

    Public Function Contains(adapter As Adapter) As Boolean Implements System.Collections.Generic.ICollection(Of Adapter).Contains
        Return list.Contains(adapter)
    End Function

    Public Sub CopyTo(array() As Adapter, arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of Adapter).CopyTo

    End Sub

    Public ReadOnly Property Count As Integer Implements System.Collections.Generic.ICollection(Of Adapter).Count
        Get
            Return list.Count
        End Get
    End Property

    Public ReadOnly Property IsReadOnly As Boolean Implements System.Collections.Generic.ICollection(Of Adapter).IsReadOnly
        Get
            Return False
        End Get
    End Property

    Public Function Remove(adapter As Adapter) As Boolean Implements System.Collections.Generic.ICollection(Of Adapter).Remove
        If adapter IsNot Nothing Then adapter.CloseAdapter()
        Return list.Remove(adapter)
    End Function

    Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of Adapter) Implements System.Collections.Generic.IEnumerable(Of Adapter).GetEnumerator
        Return list.GetEnumerator()
    End Function

    Public Function IndexOf(adapter As Adapter) As Integer Implements System.Collections.Generic.IList(Of Adapter).IndexOf
        Return list.IndexOf(adapter)
    End Function

    Public Sub Insert(index As Integer, adapter As Adapter) Implements System.Collections.Generic.IList(Of Adapter).Insert
        list.Insert(index, adapter)
    End Sub

    Default Public Property Item(index As Integer) As Adapter Implements System.Collections.Generic.IList(Of Adapter).Item
        Get
            Return list.Item(index)
        End Get
        Set(value As Adapter)
            list.Item(index) = value
        End Set
    End Property

    Public Sub RemoveAt(index As Integer) Implements System.Collections.Generic.IList(Of Adapter).RemoveAt
        list.RemoveAt(index)
    End Sub

    Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return list.GetEnumerator()
    End Function
End Class
