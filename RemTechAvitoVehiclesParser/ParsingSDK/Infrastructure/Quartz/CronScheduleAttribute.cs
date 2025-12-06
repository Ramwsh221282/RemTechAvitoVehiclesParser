namespace RemTechAvitoVehiclesParser.SharedDependencies.Quartz;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CronScheduleAttribute(string schedule) : Attribute
{
    public string Schedule { get; } = schedule;
}