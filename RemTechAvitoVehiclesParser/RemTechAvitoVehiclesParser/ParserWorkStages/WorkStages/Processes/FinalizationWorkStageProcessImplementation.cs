using ParsingSDK.Parsing;
using ParsingSDK.TextProcessing;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Processes;

public static class FinalizationWorkStageProcessImplementation
{
    extension(WorkStageProcess)
    {
        public static WorkStageProcess Finalization => async (deps, ct) =>
        {
            deps.Deconstruct(
                out BrowserFactory browsers,
                out _,
                out TextTransformerBuilder textTransformerBuilder,
                out Serilog.ILogger dLogger,
                out NpgSqlConnectionFactory npgSql
            );

            Serilog.ILogger logger = dLogger.ForContext<WorkStageProcess>();
            await using NpgSqlSession session = new(npgSql);
            NpgSqlPendingToPublishItemsStorage pendingItemsStorage = new(session);
            await session.UseTransaction(ct);

            WorkStageQuery stageQuery = new(Name: WorkStageConstants.FinalizationStage, WithLock: true);
            Maybe<ParserWorkStage> stage = await ParserWorkStage.GetSingle(session, stageQuery, ct);
            if (!stage.HasValue) return;

            PendingItemsQuery itemsQuery = new(UnprocessedOnly: true, WithLock: true, Limit: 50);
            PendingToPublishItem[] items = [.. await pendingItemsStorage.GetMany(itemsQuery, ct)];
            if (items.Length == 0)
            {
                stage.Value.ToSleepingStage();
                await stage.Value.Update(session, ct);
                await session.UnsafeCommit(ct);
                logger.Information("Switched to sleeping work stage.");
                return;
            }

            string resultsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results");
            List<PendingToPublishItem> processed = [];
            ITextTransformer transformer = textTransformerBuilder.UseSpacesCleaner().Build();

            foreach (PendingToPublishItem item in items)
            {
                string content = $"""
                                {item.Title}
                                {string.Join(" ", item.DescriptionList)}
                                {string.Join(" ", item.Characteristics)}
                                {item.Address}
                                {item.Price} {item.IsNds}
                                """;

                string transformed = transformer.TransformText(content);
                Directory.CreateDirectory(resultsDirectory);
                string filePath = Path.Combine(resultsDirectory, $"{Guid.NewGuid()}.txt");
                ResultsPublishing.Result result = ResultsPublishing.Result.CreateTextFile(transformed, filePath);
                await result.Publish(ct);
                logger.Information("Published result: {Content}", content);
                PendingToPublishItem processedItem = item.MarkProcessed();
                processed.Add(processedItem);
            }
            ;
        };
    }
}