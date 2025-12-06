using PuppeteerSharp;
using RemTechAvitoVehiclesParser.Parsing.FirewallBypass;

namespace RemTechAvitoVehiclesParser.Parsing;

public sealed class AvitoBypassFactory
{
    public IAvitoBypassFirewall Create(IPage page)
    {
        return new AvitoByPassFirewallWithRetry(new AvitoBypassFirewallLazy(page, new AvitoBypassFirewall(page)));
    }
}