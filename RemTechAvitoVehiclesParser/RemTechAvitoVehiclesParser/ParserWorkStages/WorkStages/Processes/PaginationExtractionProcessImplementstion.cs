using AvitoFirewallBypass;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Processes;

public static class PaginationExtractionProcessImplementation
{
    extension(WorkStageProcess)
    {
        public static WorkStageProcess PaginationExtraction => async (deps, ct) =>
        {
            Serilog.ILogger logger = deps.Logger.ForContext<WorkStageProcess>();
            await using NpgSqlSession session = new(deps.NpgSql);
            await session.UseTransaction(ct);
            NpgSqlPaginationParsingParsersStorage paginationStorage = new(session);            

            WorkStageQuery stageQuery = new(Name: WorkStageConstants.EvaluationStageName, WithLock: true);
            Maybe<ParserWorkStage> evalStage = await ParserWorkStage.GetSingle(session, stageQuery, ct);
            if (!evalStage.HasValue) return;

            PaginationEvaluationParsersQuery parserQuery = new(
                ParserId: evalStage.Value.Id,
                LinksWithoutCurrentPage: true,
                LinksWithoutMaxPage: true,
                WithLock: true
            );

            Maybe<PaginationParsingParser> evalutionParser = await paginationStorage.GetParser(parserQuery, ct);
            if (!evalutionParser.HasValue)
            {
                ParserWorkStage catalogue = evalStage.Value.ChangeStage(new CatalogueWorkStage(evalStage.Value));
                await catalogue.Update(session, ct);                
                await session.UnsafeCommit(ct);
                logger.Information("Switched to stage: {Name}", evalStage.Value.Name);
                return;
            }

            logger.Information("Starting extracting pagination for links.");
            IBrowser browser = await deps.Browsers.ProvideBrowser(headless: false);
            PaginationParsingParserLink[] links = [.. evalutionParser.Value.Links];

            for (int i = 0; i < links.Length; i++)
            {

                PaginationParsingParserLink link = links[i];
                logger.Information("Extracting pagination for link: {Url}", link.Url);

                try
                {
                    Maybe<IPage> page = await PreparedPage(browser, deps.Bypasses, link.Url);
                    if (!page.HasValue) throw new InvalidOperationException("Page is banned");
                    IElementHandle[] paginationElements = await GetPaginationElements(page.Value);
                    int currentPage = await GetCurrentPageFromPaginationContainer(paginationElements);
                    int maxPage = await GetMaxPageFromPaginationContainer(paginationElements);


                    link = link.AddPagination(currentPage, maxPage);
                    logger.Information(
                        """ 
                        Pagination extraction info:
                        Url: {url}
                        Current page: {Current}
                        Max page: {MaxPage}
                        """, link.Url, link.CurrentPage, link.MaxPage);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error at extracting pagination for: {Url}", link.Url);

                }
                finally
                {
                    links[i] = link;
                }
            }

            await browser.DestroyAsync();

            await paginationStorage.UpdateManyLinks(links);

            try
            {
                await session.UnsafeCommit(ct);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not commit transaction");
            }


            logger.Information("Pagination extracting finished.");
        };
    }    

    private static async Task<Maybe<IPage>> PreparedPage(IBrowser browser, AvitoBypassFactory bypasses, string url)
    {
        IPage page = await browser.GetPage();
        await page.NavigatePage(url);
        if (!await bypasses.Create(page).Bypass()) return Maybe<IPage>.None();
        await page.ScrollBottom();
        await page.ScrollTop();
        return Maybe<IPage>.Some(page);
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
