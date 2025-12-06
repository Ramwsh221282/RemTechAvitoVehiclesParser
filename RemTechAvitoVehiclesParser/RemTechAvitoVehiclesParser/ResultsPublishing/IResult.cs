namespace RemTechAvitoVehiclesParser.ResultsPublishing;

public interface IResult
{
    Task Publish(CancellationToken ct = default);
}