using System.Text.RegularExpressions;

namespace RemTechAvitoVehiclesParser.Utilities.TextTransforming;

public sealed class EmojiRemovingTextTransformer : ITextTransformer
{
    private static readonly Regex EmojiRegex = new(
        @"[\u2700-\u27BF]|" +
        @"[\u24C2-\u24FF]|" +
        @"[\uD83C][\uDDE0-\uDDFF]|" +
        @"[\uD83D][\uDC00-\uDE4F]|" +
        @"[\uD83D][\uDE80-\uDEFF]|" +
        @"[\uD83C][\uDF00-\uDFFF]|" +
        @"[\u2600-\u26FF]",
        RegexOptions.Compiled
    );
    
    public string TransformText(string text)
    {
        string cleaned = EmojiRegex.Replace(text, " ");
        return cleaned;
    }
}