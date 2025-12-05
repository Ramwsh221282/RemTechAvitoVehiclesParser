namespace RemTechAvitoVehiclesParser.ResultsPublishing;

public sealed class NullObjectFileResult : IResult
{
    public Task Publish(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}