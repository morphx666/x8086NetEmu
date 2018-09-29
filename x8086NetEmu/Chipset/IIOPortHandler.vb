Public Interface IIOPortHandler
    ReadOnly Property ValidPortAddress As List(Of UInt32)

    Sub Out(port As UInt32, value As UInt16)
    Function [In](port As UInt32) As UInt16
    ReadOnly Property Name() As String
    ReadOnly Property Description() As String
    Sub Run()
End Interface
