namespace RemTechAvitoVehiclesParser.Utilities.TextTransforming;

public sealed class NoneTextTransformer : ITextTransformer
{
    public string TransformText(string text)
    {
        return text;
    }
}