namespace RemTechAvitoVehiclesParser.Parsing.FirewallBypass;

public sealed class AvitoCaptchaImages
{
    private readonly List<byte[]> _bytes;

    public AvitoCaptchaImages()
    {
        _bytes = new List<byte[]>(2);
    }

    public AvitoCaptchaImages(AvitoCaptchaImages origin, byte[] imageBytes)
    {
        origin._bytes.Add(imageBytes);
        _bytes = origin._bytes;
    }

    public AvitoCaptchaImages With(byte[] bytes) =>
        _bytes.Count == 2 ? this : new AvitoCaptchaImages(this, bytes);

    public bool Filled() => _bytes.Count == 2;

    public byte[] ReadMin()
    {
        byte[] bytes = _bytes.MinBy(b => b.Length)!;
        return bytes;
    }

    public byte[] ReadMax()
    {
        byte[] bytes = _bytes.MaxBy(b => b.Length)!;
        return bytes;
    }
}