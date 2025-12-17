namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common;

public sealed record AvitoVehicleCatalogueRepresentation(
    string Id,
    string Url,
    long Price,
    bool IsNds,
    string Address,
    string[] Photos
)
{
    public static AvitoVehicleCatalogueRepresentation Empty() =>
        new(
            string.Empty,
            string.Empty,
            -1,
            false,
            string.Empty,
            Array.Empty<string>()
        );

    public bool IsEmpty() =>
        Id == string.Empty && Url == string.Empty && Price == -1 && IsNds == false && Address == string.Empty && Photos.Length == 0;
}