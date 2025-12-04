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
            services.RegisterPaginationEvaluationBackgroundJob();
            services.RegisterSwitchToCatalogueStageBackgroundJob();
            services.AddProcessParserUrlsBackgroundTask();
        }
        
        public void RegisterStorage()
        {
            services.AddScoped<NpgSqlParserWorkStagesStorage>();
            services.AddScoped<NpgSqlPaginationEvaluationParsersStorage>();
            services.AddScoped<NpgSqlCataloguePageUrlsStorage>();
        }

        public void AddProcessParserUrlsBackgroundTask()
        {
            services.AddSingleton<ICronScheduleJob, ProcessParserUrlsBackgroundTask>();
        }
        
        public void RegisterPaginationEvaluationBackgroundJob()
        {
            services.AddSingleton<ICronScheduleJob, CreatePaginationEvaluationBackgroundTask>();
        }

        public void RegisterSwitchToCatalogueStageBackgroundJob()
        {
            services.AddSingleton<ICronScheduleJob, SwitchToCatalogueStageBackgroundTask>();
        }
        
        public void AddSaveEvaluationParserWorkStageCommand()
        {
            services.AddScoped<ISaveEvaluationParserWorkStage, SaveEvaluationParserWorkStage>();
            services.Decorate<ISaveEvaluationParserWorkStage, SaveEvaluationParserWorkStageLogging>();
            services.Decorate<ISaveEvaluationParserWorkStage, SaveEvaluationParserWorkStageTransaction>();
        }
    }
}