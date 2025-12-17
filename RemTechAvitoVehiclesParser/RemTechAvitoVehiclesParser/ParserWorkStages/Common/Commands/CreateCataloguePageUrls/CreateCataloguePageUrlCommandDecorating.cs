namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.CreateCataloguePageUrls;

public static class CreateCataloguePageUrlCommandDecorating
{
    extension(ICreateCataloguePageUrlsCommand command)
    {
        public ICreateCataloguePageUrlsCommand UseLogging(Serilog.ILogger logger)
            => new CreateCataloguePageUrlsLogging(logger, command);
    }
}