Public Interface IIOPortHandler
    ReadOnly Property ValidPortAddress As List(Of UInteger)

    Sub Out(port As UInteger, value As UInteger)
    Function [In](port As UInteger) As UInteger
    ReadOnly Property Name() As String
    ReadOnly Property Description() As String
    Sub Run()
End Interface
