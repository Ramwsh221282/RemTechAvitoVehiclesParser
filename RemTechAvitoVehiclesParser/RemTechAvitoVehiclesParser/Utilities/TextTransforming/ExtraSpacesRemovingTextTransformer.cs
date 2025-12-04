using System.Text.RegularExpressions;

namespace RemTechAvitoVehiclesParser.Utilities.TextTransforming;

public sealed partial class ExtraSpacesRemovingTextTransformer : ITextTransformer
{
    private static readonly Regex ExtraSpacesRegex = Regex();
    
    public string TransformText(string text)
    {
        return Regex().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex Regex();
}

public sealed class NoneTextTransformer : ITextTransformer
{
    public string TransformText(string text)
    {
        return text;
    }
}