namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common;

public sealed record AvitoVehicleConcretePageRepresentation(string Title, Dictionary<string, string> Characteristics)
{
    public static AvitoVehicleConcretePageRepresentation Empty() => new(string.Empty, []);
    
    public bool IsEmpty() => Title == string.Empty && Characteristics.Count == 0;
}