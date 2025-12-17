namespace RemTechAvitoVehiclesParser.ResultsPublishing;

public sealed class Result
{
    private IResult TextFileResult { get; init; } = new NullObjectFileResult();

    public async Task Publish(CancellationToken ct = default)
    {
        await TextFileResult.Publish(ct);
    }

    public static Result CreateTextFile(string contents, string filePath)
    {
        return new Result() { TextFileResult = new TextFileResult(contents, filePath) };
    }

    private Result() { }
}