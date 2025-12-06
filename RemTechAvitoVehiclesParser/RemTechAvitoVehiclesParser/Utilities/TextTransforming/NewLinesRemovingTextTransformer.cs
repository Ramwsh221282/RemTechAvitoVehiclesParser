using System.Text.RegularExpressions;

namespace RemTechAvitoVehiclesParser.Utilities.TextTransforming;

public sealed partial class NewLinesRemovingTextTransformer : ITextTransformer
{
    private static readonly Regex NewLineRegex = Regex();
    
    public string TransformText(string text)
    {
        return Regex().Replace(text, " ");
    }

    [GeneratedRegex(@"\r\n|\r|\n", RegexOptions.Compiled)]
    private static partial Regex Regex();
}