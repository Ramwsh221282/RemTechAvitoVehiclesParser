using System.Text.RegularExpressions;

namespace RemTechAvitoVehiclesParser.Utilities.TextTransforming;

public sealed class EmojiRemovingTextTransformer : ITextTransformer
{
    public string TransformText(string text)
    {
        string emojiPattern = @"[\u1F600-\u1F64F\u1F300-\u1F5FF\u1F680-\u1F6FF\u1F1E0-\u1F1FF\u2702-\u27B0\u24C2-\u1F251]+";
        string cleaned = Regex.Replace(text, emojiPattern, " ");
        return cleaned;
    }
}