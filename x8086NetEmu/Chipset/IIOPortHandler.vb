Public Interface IIOPortHandler
    ReadOnly Property ValidPortAddress As List(Of Integer)

    Sub Out(port As Integer, value As Integer)
    Function [In](port As Integer) As Integer
    ReadOnly Property Name() As String
    ReadOnly Property Description() As String
    Sub Run()
End Interface
