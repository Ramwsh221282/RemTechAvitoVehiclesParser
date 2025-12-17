namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.ExtractConcreteItem;

public interface IExtractConcreteItemCommand
{
    Task<AvitoVehicle> Handle();
}