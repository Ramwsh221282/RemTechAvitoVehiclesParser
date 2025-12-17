using RemTech.SharedKernel.Infrastructure.Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages;

namespace RemTechAvitoVehiclesParser.ParserWorkStages;

public static class ParserWorkStagesDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserWorkStagesContext()
        {            
            services.AddTransient<ICronScheduleJob, WorkStageProcessInvoker>();
            services.AddTransient<WorkStageProcessDependencies>();
        }                
    }
}