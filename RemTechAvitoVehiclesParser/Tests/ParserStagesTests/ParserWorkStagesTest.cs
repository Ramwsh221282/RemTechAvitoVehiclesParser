using Microsoft.Extensions.DependencyInjection;

namespace Tests.ParserStagesTests;

public sealed class ParserWorkStagesTest(IntegrationalTestsFixture fixture) : IClassFixture<IntegrationalTestsFixture>
{
    private IServiceProvider Services { get; } = fixture.Services;
    const string targetUrl = "https://www.avito.ru/all/gruzoviki_i_spetstehnika/tehnika_dlya_lesozagotovki/john_deere-ASgBAgICAkRUsiyexw3W6j8?cd=1";
    
    [Fact]
    public async Task Test_Parser_Work_Stages()
    {
        await PublishStartParser();
        await Task.Delay(TimeSpan.FromMinutes(30));
    }
    
    private async Task PublishStartParser()
    {
        FakeParserData data = new(Guid.NewGuid(), "Техника", "Avito");
        FakeParserLinkData link = new(Guid.NewGuid(), targetUrl);
        FakeStartParserPublisher publisher = Services.GetRequiredService<FakeStartParserPublisher>();
        await publisher.Publish(new FakeParserPublishPayload(data, new[] { link }));
    }
}