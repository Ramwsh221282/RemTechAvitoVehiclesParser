namespace RemTechAvitoVehiclesParser.ResultsPublishing;

public sealed class TextFileResult(string contents, string filePath) : IResult
{
    public async Task Publish(CancellationToken ct = default)
    {
        await using StreamWriter writer = File.CreateText(filePath);
        await writer.WriteLineAsync(contents);
    }
}