using PuppeteerSharp;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.Utils;

namespace RemTechAvitoVehiclesParser.FirewallBypass;

public sealed class AvitoBypassFirewallLazy(IPage page, IAvitoBypassFirewall origin) : IAvitoBypassFirewall
{
    public async Task<bool> Bypass()
    {
        if (await FirewallDoesNotExist())
            return true;
        await origin.Bypass();
        return await FirewallDoesNotExist();
    }

    private async Task<bool> FirewallDoesNotExist()
    {
        Maybe<IElementHandle> firewall = await page.GetElementRetriable(".firewall-title", retryAmount: 10);
        return firewall.HasValue == false;
    }
}