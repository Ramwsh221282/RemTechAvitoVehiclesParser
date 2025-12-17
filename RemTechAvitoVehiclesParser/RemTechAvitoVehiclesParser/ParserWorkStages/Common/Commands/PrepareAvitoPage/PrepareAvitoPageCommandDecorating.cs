namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.PrepareAvitoPage;

public static class PrepareAvitoPageCommandDecorating
{
    extension(IPrepareAvitoPageCommand command)
    {
        public IPrepareAvitoPageCommand UseLogging(Serilog.ILogger logger)
            => new PrepareAvitoPageLogging(logger, command);        
    }
}