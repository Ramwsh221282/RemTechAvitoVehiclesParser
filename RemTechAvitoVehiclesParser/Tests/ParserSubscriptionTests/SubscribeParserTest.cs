using Microsoft.Extensions.DependencyInjection;
using ParserSubscriber.SubscribtionContext;

namespace Tests.ParserSubscriptionTests;

public sealed class SubscribeParserTest(ParserSubscriptionFixture fixture) : IClassFixture<ParserSubscriptionFixture>
{
    private IServiceProvider Services { get; } = fixture.Services;
    
    [Fact]
    private async Task Test_Parser_Subscription_Success()
    {
        await Services.RunParserSubscription();
        bool hasSubscription = await HasParserSubscription();
        Assert.True(hasSubscription);
    }
    
    private async Task<bool> HasParserSubscription()
    {
        SubscriptionStorage storage = Services.GetRequiredService<SubscriptionStorage>();
        ParserSubscribtion? subscription = await storage.GetSubscription();
        return subscription != null;
    }
}