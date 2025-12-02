using PuppeteerSharp;
using PuppeteerSharp.Input;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.Utils;

namespace RemTechAvitoVehiclesParser.FirewallBypass;

public sealed class AvitoCaptchaSliderMovement(IPage page, int centerPoint)
{
    public async Task Move()
    {
        Maybe<IElementHandle> slider = await page.GetElementRetriable(".geetest_btn", retryAmount: 10);
        if (!slider.HasValue)
            return;

        await Task.Delay(TimeSpan.FromSeconds(5));
        MoveOptions moveOptions = new() { Steps = 25 }; 
        BoundingBox bbox = await slider.Value.BoundingBoxAsync();
        decimal xPosition = bbox.X + (bbox.Width / 2);
        decimal yPosition = bbox.Y + (bbox.Height / 2);
        await page.Mouse.MoveAsync(xPosition, yPosition, moveOptions);
        await page.Mouse.DownAsync();
        decimal xPositionN = bbox.X + centerPoint;
        decimal yPositionN = bbox.Y;
        await page.Mouse.MoveAsync(xPositionN, yPositionN, moveOptions);
        await Task.Delay(500);
        await page.Mouse.UpAsync();
        await Task.Delay(TimeSpan.FromSeconds(10));
        await slider.Value.DisposeAsync();
    }
}