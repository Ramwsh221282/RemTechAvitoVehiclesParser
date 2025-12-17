namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.ExtractConcreteItem;

public static class ExtractConcreteItemDecorating
{
    extension(IExtractConcreteItemCommand command)
    {
        public IExtractConcreteItemCommand UseLogging(Serilog.ILogger logger)
            => new ExtractConcreteItemLogging(logger, command);
    }
}