namespace RemTechAvitoVehiclesParser.Utilities.TextTransforming;

public sealed class TextTransformerBuilder
{
    private ITextTransformer _transformer = new NoneTextTransformer();

    public TextTransformerBuilder UsePunctuationCleaner()
    {
        _transformer = new ChainedTextTransformer(_transformer, new PunctuationsRemovingTextTransformer());
        return this;
    }

    public TextTransformerBuilder UseNewLinesCleaner()
    {
        _transformer = new ChainedTextTransformer(_transformer, new NewLinesRemovingTextTransformer());
        return this;
    }

    public TextTransformerBuilder UseSpacesCleaner()
    {
        _transformer = new ChainedTextTransformer(_transformer, new ExtraSpacesRemovingTextTransformer());
        return this;
    }

    public TextTransformerBuilder UseEmojiCleaner()
    {
        _transformer = new ChainedTextTransformer(_transformer, new EmojiRemovingTextTransformer());
        return this;
    }
    
    public ITextTransformer Build()
    {
        ITextTransformer nextDefault = new NoneTextTransformer();
        ITextTransformer transformer = _transformer;
        _transformer = nextDefault;
        return transformer;
    }
}