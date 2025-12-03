using Microsoft.Extensions.DependencyInjection;
using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace Tests.ParserWorkStartTests;

public sealed class StartParserWorkTest(ParserWorkStartFixture fixture) : IClassFixture<ParserWorkStartFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;

    [Fact]
    private async Task Start_Parser_Work_Test_Success()
    {
        Guid id = Guid.NewGuid();
        await PublishMessageToStartParserWork(id);
        await Task.Delay(TimeSpan.FromSeconds(10));
        bool hasWorkStage = await EnsureHasParserEvaluationStage();
        Assert.True(hasWorkStage);
    }

    private async Task PublishMessageToStartParserWork(Guid id)
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        TestStartParserWorkPublisher publisher =
            scope.ServiceProvider.GetRequiredService<TestStartParserWorkPublisher>();
        await publisher.Publish(id);
    }

    private async Task<bool> EnsureHasParserEvaluationStage()
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        NpgSqlParserWorkStagesStorage storage =
            scope.ServiceProvider.GetRequiredService<NpgSqlParserWorkStagesStorage>();
        ParserWorkStageQuery query = new(Name: "EVALUATION");
        Maybe<ParserWorkStage> stage = await storage.GetWorkStage(query);
        return stage.HasValue;
    }
}