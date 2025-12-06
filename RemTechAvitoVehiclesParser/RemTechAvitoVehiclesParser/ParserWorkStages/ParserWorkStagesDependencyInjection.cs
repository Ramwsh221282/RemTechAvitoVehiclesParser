using RemTech.SharedKernel.Infrastructure.Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.BackgroundTasks;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.BackgroundTasks;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.BackgroundTasks;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Features.SaveEvaluationParserWorkStage;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Features.SaveEvaluationParserWorkStage.Decorators;

namespace RemTechAvitoVehiclesParser.ParserWorkStages;

public static class ParserWorkStagesDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserWorkStagesContext()
        {
            services.RegisterStorage();
            services.AddSaveEvaluationParserWorkStageCommand();
            services.AddParserProcessingJobs();
        }
        
        public void RegisterStorage()
        {
            services.AddScoped<NpgSqlParserWorkStagesStorage>();
            services.AddScoped<NpgSqlPaginationParsingParsersStorage>();
            services.AddScoped<NpgSqlCataloguePageUrlsStorage>();
        }

        public void AddParserProcessingJobs()
        {
            services.AddSingleton<ICronScheduleJob, CataloguePagesPagesParsingBackgroundTask>();
            services.AddSingleton<ICronScheduleJob, CataloguePaginationParsingBackgroundTask>();
            services.AddSingleton<ICronScheduleJob, SwitchToCatalogueStageBackgroundTask>();
            services.AddSingleton<ICronScheduleJob, ConcretePagesProcessingBackgroundTask>();
        }
        
        public void AddSaveEvaluationParserWorkStageCommand()
        {
            services.AddScoped<ISaveEvaluationParserWorkStage, SaveEvaluationParserWorkStage>();
            services.Decorate<ISaveEvaluationParserWorkStage, SaveEvaluationParserWorkStageLogging>();
            services.Decorate<ISaveEvaluationParserWorkStage, SaveEvaluationParserWorkStageTransaction>();
        }
    }
}