using ParsingSDK;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace RemTechAvitoVehiclesParser.Parsing.FirewallBypass;

public sealed class AvitoBypassFirewall(IPage page) : IAvitoBypassFirewall
{
    public async Task<bool> Bypass()
    {
        Maybe<IElementHandle> form = await page.GetElementRetriable(".form-action");
        AvitoCaptchaImagesInterception interception = new(page, form);
        AvitoCaptchaImages images = await interception.Intercept();
        AvitoCaptchaMovementPosition position = new(images);
        int centerPoint = position.CenterPoint();
        await new AvitoCaptchaSliderMovement(page, centerPoint).Move();
        if (form.HasValue) await form.Value.DisposeAsync();
        return false;
    }
}