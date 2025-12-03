using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Features.SaveEvaluationParserWorkStage;
using RemTechAvitoVehiclesParser.ParserWorkStages.Features.SaveEvaluationParserWorkStage.Decorators;

namespace RemTechAvitoVehiclesParser.ParserWorkStages;

public static class ParserWorkStagesDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserWorkStagesContext()
        {
            services.RegisterStorage();
            services.AddSaveEvaluationParserWorkStageCommand();
        }
        
        public void RegisterStorage()
        {
            services.AddScoped<NpgSqlParserWorkStagesStorage>();
            services.AddScoped<NpgSqlPaginationEvaluationParsersStorage>();
        }

        public void AddSaveEvaluationParserWorkStageCommand()
        {
            services.AddScoped<ISaveEvaluationParserWorkStage, SaveEvaluationParserWorkStage>();
            services.Decorate<ISaveEvaluationParserWorkStage, SaveEvaluationParserWorkStageLogging>();
            services.Decorate<ISaveEvaluationParserWorkStage, SaveEvaluationParserWorkStageTransaction>();
        }
    }
}