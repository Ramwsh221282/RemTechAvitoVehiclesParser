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
        IEnumerable<(Guid, string)> links = [(Guid.NewGuid(), "https://www.avito.ru/all/gruzoviki_i_spetstehnika/tehnika_dlya_lesozagotovki/ponsse-ASgBAgICAkRUsiyexw346j8?cd=1")];
        
        await PublishMessageToStartParserWork(id, domain, type, links);
        await Task.Delay(TimeSpan.FromSeconds(10));
        bool hasWorkStage = await EnsureHasParserEvaluationStage();
        bool hasParser = await EnsureHasPendingParserPaginationEvaluation(id);
        Assert.True(hasWorkStage);
        Assert.True(hasParser);
        await Task.Delay(TimeSpan.FromMinutes(1));
        bool hasPaginationEvaluated = await EnsurePaginationEvaluated(id);
        bool hasCatalogueStage = await EnsureHasCatalogueStage(id);
        Assert.True(hasPaginationEvaluated);
        await Task.Delay(TimeSpan.FromHours(1));
    }

    private async Task PublishMessageToStartParserWork(Guid id, string domain, string type, IEnumerable<(Guid, string)> links)
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        TestStartParserWorkPublisher publisher = scope.ServiceProvider.GetRequiredService<TestStartParserWorkPublisher>();
        await publisher.Publish(id, domain, type, links);
    }

    public async Task<bool> EnsureHasCatalogueStage(Guid id)
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        NpgSqlParserWorkStagesStorage workStages = scope.ServiceProvider.GetRequiredService<NpgSqlParserWorkStagesStorage>();
        ParserWorkStageQuery query = new(Id: id, Name: WorkStageConstants.CatalogueStageName);
        Maybe<ParserWorkStage> stage = await workStages.GetWorkStage(query);
        return stage.HasValue;
    }
    
    private async Task<bool> EnsurePaginationEvaluated(Guid id)
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        NpgSqlPaginationEvaluationParsersStorage storage = scope.ServiceProvider.GetRequiredService<NpgSqlPaginationEvaluationParsersStorage>();
        PaginationEvaluationParsersQuery query = new(ParserId: id, LinksWithCurrentPage: true, LinksWithMaxPage: true);
        Maybe<PaginationEvaluationParser> parser = await storage.GetParser(query);
        return parser.HasValue;
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
        NpgSqlParserWorkStagesStorage storage = scope.ServiceProvider.GetRequiredService<NpgSqlParserWorkStagesStorage>();
        ParserWorkStageQuery query = new(Name: "EVALUATION");
        Maybe<ParserWorkStage> stage = await storage.GetWorkStage(query);
        return stage.HasValue;
    }
}