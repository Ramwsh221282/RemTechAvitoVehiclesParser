namespace RemTechAvitoVehiclesParser.FirewallBypass;

public sealed class AvitoByPassFirewallWithRetry(IAvitoBypassFirewall firewall, int retry = 5) : IAvitoBypassFirewall
{
    public async Task<bool> Bypass()
    {
        int currentAttempt = 0;
        while (currentAttempt < retry)
        {
            bool resolved = await firewall.Bypass();
            if (resolved) return resolved;
            currentAttempt++;
        }
        
        return false;
    }
}