using RemTechAvitoVehiclesParser.ParserWorkStages.BackgroundTasks;
using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Features.SaveEvaluationParserWorkStage;
using RemTechAvitoVehiclesParser.ParserWorkStages.Features.SaveEvaluationParserWorkStage.Decorators;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;

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
            services.AddScoped<NpgSqlPaginationEvaluationParsersStorage>();
            services.AddScoped<NpgSqlCataloguePageUrlsStorage>();
        }

        public void AddParserProcessingJobs()
        {
            services.AddSingleton<ICronScheduleJob, CataloguePagesProcessingBackgroundTask>();
            services.AddSingleton<ICronScheduleJob, CataloguePagesEvaluationBackgroundTask>();
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