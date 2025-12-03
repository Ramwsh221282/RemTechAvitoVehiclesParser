using Microsoft.Extensions.DependencyInjection;
using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace Tests.ParserWorkStartTests;

public sealed class StartParserWorkTest(ParserWorkStartFixture fixture) : IClassFixture<ParserWorkStartFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;

    [Fact]
    private async Task Start_Parser_Work_Test_Success()
    {
        Guid id = Guid.NewGuid();
        string domain = ConstantsForMainApplicationCommunication.CurrentServiceDomain;
        string type = ConstantsForMainApplicationCommunication.CurrentServiceType;
        IEnumerable<(Guid, string)> links = [(Guid.NewGuid(), "https://www.avito.ru/all/gruzoviki_i_spetstehnika/tehnika_dlya_lesozagotovki-ASgBAgICAURUsiw")];
        
        await PublishMessageToStartParserWork(id, domain, type, links);
        await Task.Delay(TimeSpan.FromSeconds(10));
        bool hasWorkStage = await EnsureHasParserEvaluationStage();
        bool hasParser = await EnsureHasPendingParserPaginationEvaluation(id); // TODO FIX PARSING FROM DB ROWS.
        Assert.True(hasWorkStage);
        Assert.True(hasParser);
    }

    private async Task PublishMessageToStartParserWork(Guid id, string domain, string type, IEnumerable<(Guid, string)> links)
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        TestStartParserWorkPublisher publisher =
            scope.ServiceProvider.GetRequiredService<TestStartParserWorkPublisher>();
        await publisher.Publish(id, domain, type, links);
    }

    private async Task<bool> EnsureHasPendingParserPaginationEvaluation(Guid id)
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        PaginationEvaluationParsersQuery query = new(ParserId: id);
        NpgSqlPaginationEvaluationParsersStorage storage = scope.ServiceProvider.GetRequiredService<NpgSqlPaginationEvaluationParsersStorage>();
        Maybe<PaginationEvaluationParser> parser = await storage.GetParser(query);
        return parser.HasValue;
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