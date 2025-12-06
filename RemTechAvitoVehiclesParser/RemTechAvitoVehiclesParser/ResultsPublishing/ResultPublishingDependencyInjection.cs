using RemTechAvitoVehiclesParser.ResultsPublishing.BackgroundTasks;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;

namespace RemTechAvitoVehiclesParser.ResultsPublishing;

public static class ResultPublishingDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterResultPublishing()
        {
            services.AddSingleton<ICronScheduleJob, ResultsPublishingBackgroundTask>();
        }
    }
}