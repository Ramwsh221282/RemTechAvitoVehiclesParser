using RemTech.SharedKernel.Infrastructure.Quartz;
using RemTechAvitoVehiclesParser.ResultsPublishing.BackgroundTasks;

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