namespace RemTechAvitoVehiclesParser.Utilities.TextTransforming;

public sealed class ChainedTextTransformer(ITextTransformer current, ITextTransformer? next = null) : ITextTransformer
{
    public string TransformText(string text)
    {
        string transformed = current.TransformText(text);
        return next == null ? transformed : next.TransformText(transformed);
    }
}