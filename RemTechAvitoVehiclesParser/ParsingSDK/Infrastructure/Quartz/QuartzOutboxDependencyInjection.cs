using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace RemTechAvitoVehiclesParser.SharedDependencies.Quartz;

public static class QuartzOutboxDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void AddCronScheduledJobs()
        {
            Type[] jobTypes = services
                .Where(s => s.ServiceType == typeof(ICronScheduleJob)).Select(s => s.ImplementationType)
                .Where(t => t != null)
                .Cast<Type>()
                .ToArray();

            if (jobTypes.Length == 0)
                throw new ApplicationException(
                    $"Cannot register Cron schedule jobs. There are no {nameof(ICronScheduleJob)} instances registered in service collection.");
            
            services.AddQuartz(q =>
            {
                foreach (var jobType in jobTypes)
                {
                    string typeName = jobType.Name;
                    CronScheduleAttribute? cronSchedule = jobType.GetCustomAttribute<CronScheduleAttribute>();
                    if (cronSchedule == null) 
                        throw new ApplicationException($"{typeName} is implementing: {nameof(ICronScheduleJob)} but has no {nameof(CronScheduleAttribute)} on it.");
                    
                    JobKey jobKey = new(typeName);
                    TriggerKey triggerKey = new($"trigger_{typeName}");
                    q.AddJob(jobType, jobKey, j => j.StoreDurably());
                    q.AddTrigger(t => t.ForJob(jobKey).WithIdentity(triggerKey).WithCronSchedule(cronSchedule.Schedule));
                }
            });
        }
    }
}