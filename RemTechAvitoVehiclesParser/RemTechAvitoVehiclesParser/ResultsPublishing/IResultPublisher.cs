namespace RemTechAvitoVehiclesParser.ResultsPublishing;

public interface IResultPublisher<in TResult> where TResult : IResult
{
    Task Publish(TResult result, CancellationToken ct = default);
}