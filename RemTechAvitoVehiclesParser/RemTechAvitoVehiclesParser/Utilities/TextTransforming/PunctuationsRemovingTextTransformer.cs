using System.Text.RegularExpressions;

namespace RemTechAvitoVehiclesParser.Utilities.TextTransforming;

public sealed partial class PunctuationsRemovingTextTransformer : ITextTransformer
{
    private static readonly Regex PunctuationRegex = Regex();
    
    public string TransformText(string text)
    {
        return Regex().Replace(text, " ");
    }

    [GeneratedRegex(@"\p{P}", RegexOptions.Compiled)]
    private static partial Regex Regex();
}