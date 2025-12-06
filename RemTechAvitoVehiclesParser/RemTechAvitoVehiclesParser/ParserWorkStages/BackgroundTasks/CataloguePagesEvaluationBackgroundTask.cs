using ParsingSDK;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class CataloguePagesEvaluationBackgroundTask(
    Serilog.ILogger logger,
    NpgSqlDataSourceFactory dataSourceFactory,
    BrowserFactory browserFactory,
    AvitoBypassFactory bypassFactory
    ) : 
    ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = logger.ForContext<CataloguePagesEvaluationBackgroundTask>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        await using IPostgreSqlAdapter session = await dataSourceFactory.CreateAdapter(ct);
        await session.UseTransaction(ct);
        NpgSqlPaginationEvaluationParsersStorage paginations = new(session);
        NpgSqlParserWorkStagesStorage workStages = new(session);
        
        ParserWorkStageQuery stageQuery = new(Name: WorkStageConstants.EvaluationStageName, WithLock: true);
        Maybe<ParserWorkStage> evaluationStage = await workStages.GetWorkStage(stageQuery, ct);
        if (!evaluationStage.HasValue) return;
            
        Guid stageId = evaluationStage.Value.GetSnapshot().Id;
        PaginationEvaluationParsersQuery parsersQuery = new(ParserId: stageId, LinksWithoutCurrentPage: true, LinksWithoutMaxPage: true, WithLock: true);
        Maybe<PaginationEvaluationParser> evaluationParser = await paginations.GetParser(parsersQuery, ct);
        if (!evaluationParser.HasValue) return;
        
        _logger.Information("Evaluation work stage and parser detected. Starting evaluating pagination.");
        await ProcessPaginationEvaluationForUrls(paginations, evaluationParser.Value.GetSnapshot());

        try
        {
            await session.CommitTransaction(ct);
        }
        catch(Exception ex)
        {
            _logger.Error(ex, "Failed to evaluate pagination for URLs. Transaction error.");
        }
    }

    private async Task ProcessPaginationEvaluationForUrls(
        NpgSqlPaginationEvaluationParsersStorage paginations,
        PaginationEvaluationParserSnapshot parserSnapshot, 
        CancellationToken ct = default)
    {
        foreach (var link in parserSnapshot.Links)
        {
            IBrowser browser = await browserFactory.ProvideBrowser(headless: false);
            _logger.Information("Evaluating pagination for: {Url}", link.Url);
            
            try
            {
                await (await browser.GetPage()).NavigatePage(link.Url);
                if (!await bypassFactory.Create(await browser.GetPage()).Bypass())
                {
                    _logger.Warning("Failed evaliation pagination for {Url}. Captcha was not solved", link.Url);
                    return;
                }

                await (await browser.GetPage()).ScrollBottom();
                await (await browser.GetPage()).ScrollTop();
                IElementHandle[] paginationElements = await GetPaginationElements(await browser.GetPage());
                int currentPage = await GetCurrentPageFromPaginationContainer(paginationElements);
                int maxPage = await GetMaxPageFromPaginationContainer(paginationElements);

                PaginationEvaluationParserLink evaluationParserLink = PaginationEvaluationParserLink.FromSnapshot(link);
                PaginationEvaluationParserLink withPagination = evaluationParserLink.AddPagination(currentPage, maxPage);
                await paginations.UpdateLink(withPagination, parserSnapshot.Id, ct);

                _logger.Information("""
                                    Evaluated pagination info:
                                    URL: {Url} 
                                    Current page: {CurrentPage}
                                    Max page: {MaxPage}
                                    """, link.Url, currentPage, maxPage);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error at evaliting pagination for url: {Url}", link.Url);
            }
            
            await browser.DestroyAsync();
        }
        
    }
    
    private static async Task<IElementHandle[]> GetPaginationElements(IPage page)
    {
        Maybe<IElementHandle> element = await page.GetElementRetriable("nav[aria-label='Пагинация']");
        if (!element.HasValue) return [];
        IElementHandle[] paginationElements = await element.Value.GetElements("li");
        if (paginationElements.Length == 0) return [];
        return paginationElements;
    }
    
    private static async Task<int> GetCurrentPageFromPaginationContainer(IElementHandle[] elements)
    {
        int currentPage = 0;
        foreach (IElementHandle element in elements)
        {
            Maybe<IElementHandle> selectedPage = await element.GetElementRetriable("span[aria-current='page']");
            if (!selectedPage.HasValue) continue;
            Maybe<IElementHandle> pageNumberElement = await selectedPage.Value.GetElementRetriable("span.styles-module-text-Z0vDE");
            if (!pageNumberElement.HasValue) continue;
            Maybe<string> pageNumberText = await pageNumberElement.Value.GetElementInnerText();
            if (!pageNumberText.HasValue) continue;
            currentPage = int.Parse(pageNumberText.Value);
            break;
        }

        return currentPage;
    }
    
    private static async Task<int> GetMaxPageFromPaginationContainer(IElementHandle[] elements)
    {
        int maxPage = 0;
        foreach (IElementHandle element in elements)
        {
            Maybe<IElementHandle> pageNumberElement = await element.GetElementRetriable("span.styles-module-text-Z0vDE");
            if (!pageNumberElement.HasValue) continue;
            Maybe<string> pageNumberText = await pageNumberElement.Value.GetElementInnerText();
            if (!pageNumberText.HasValue) continue;
            if (!int.TryParse(pageNumberText.Value, out int maxPageValue)) continue;
            if (maxPage < maxPageValue) maxPage = maxPageValue;
        }
        
        return maxPage;
    }
}