using PuppeteerSharp;
using Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.Parsing.FirewallBypass;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class CreatePaginationEvaluationBackgroundTask(
    Serilog.ILogger logger,
    NpgSqlDataSourceFactory dataSourceFactory,
    BrowserFactory factory
    ) : 
    ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = logger.ForContext<CreatePaginationEvaluationBackgroundTask>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        _logger.Information("Starting create pagination evaluation background job.");
        await using (IPostgreSqlAdapter session = await dataSourceFactory.CreateAdapter(ct))
        {
            await session.UseTransaction(ct);
            NpgSqlPaginationEvaluationParsersStorage paginations = new(session);
            NpgSqlParserWorkStagesStorage workStages = new(session);
            
            
            ParserWorkStageQuery stageQuery = new(Name: WorkStageConstants.EvaluationStageName, WithLock: true);
            Maybe<ParserWorkStage> evaluationStage = await workStages.GetWorkStage(stageQuery, ct);
            if (!evaluationStage.HasValue)
            {
                _logger.Information("No evaluation work stage exists. Stopping job.");
                return;
            }
            
            Guid stageId = evaluationStage.Value.GetSnapshot().Id;
            PaginationEvaluationParsersQuery parsersQuery = new(ParserId: stageId, LinksWithoutCurrentPage: true, LinksWithoutMaxPage: true, WithLock: true);
            Maybe<PaginationEvaluationParser> evaluationParser = await paginations.GetParser(parsersQuery, ct);
            if (!evaluationParser.HasValue)
            {
                _logger.Information("No pagination evaluation parser exists. Stopping job.");
                return;
            }
            
            
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
    }

    private async Task ProcessPaginationEvaluationForUrls(
        NpgSqlPaginationEvaluationParsersStorage paginations,
        PaginationEvaluationParserSnapshot parserSnapshot, 
        CancellationToken ct = default)
    {
        foreach (var link in parserSnapshot.Links)
        {
            await using (IBrowser browser = await factory.ProvideBrowser(headless: false))
            {
                _logger.Information("Evaluating pagination for: {Url}", link.Url);
                try
                {
                    IPage page = await browser.GetPage();
                    await page.NavigatePage(link.Url);
                    bool solved = await new AvitoByPassFirewallWithRetry(new AvitoBypassFirewallLazy(page, new AvitoBypassFirewall(page))).Bypass();
                    if (!solved)
                    {
                        _logger.Warning("Failed evaliation pagination for {Url}. Captcha was not solved", link.Url);
                        return;
                    }
                    
                    await page.ScrollBottom();
                    await page.ScrollTop();
                    IElementHandle[] paginationElements = await GetPaginationElements(page);
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
                catch(Exception ex)
                {
                    _logger.Error(ex, "Error at evaliting pagination for url: {Url}", link.Url);
                }
            }
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
            if (maxPage < maxPageValue)
                maxPage = maxPageValue;
        }
        
        return maxPage;
    }
}