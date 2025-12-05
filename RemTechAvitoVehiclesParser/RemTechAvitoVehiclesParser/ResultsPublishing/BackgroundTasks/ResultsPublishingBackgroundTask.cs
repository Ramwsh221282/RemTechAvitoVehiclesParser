using Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;
using RemTechAvitoVehiclesParser.Utilities.TextTransforming;

namespace RemTechAvitoVehiclesParser.ResultsPublishing.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class ResultsPublishingBackgroundTask(
    NpgSqlDataSourceFactory dataSourceFactory,
    Serilog.ILogger logger,
    TextTransformerBuilder textTransformerBuilder
    ) : ICronScheduleJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        await using IPostgreSqlAdapter adapter = await dataSourceFactory.CreateAdapter(ct);
        NpgSqlParserWorkStagesStorage stagesStorage = new(adapter);
        NpgSqlPendingToPublishItemsStorage pendingItemsStorage = new(adapter);

        await adapter.UseTransaction(ct);

        ParserWorkStageQuery stageQuery = new(Name: WorkStageConstants.FinalizationStage, WithLock: true );
        Maybe<ParserWorkStage> stage = await stagesStorage.GetWorkStage(stageQuery, ct);
        if (!stage.HasValue) return;

        PendingItemsQuery itemsQuery = new(UnprocessedOnly: true, WithLock: true, Limit: 50);
        PendingToPublishItem[] items = (await pendingItemsStorage.GetMany(itemsQuery, ct)).ToArray();
        if (items.Length == 0)
        {
            ParserWorkStage sleeping = stage.Value.ChangeStage(new ParserWorkStage.SleepingWorkStage(stage.Value));
            await stagesStorage.Update(sleeping, ct);
            await adapter.CommitTransaction(ct);
            logger.Information("Switched to sleeping work stage.");
            return;
        }
        
        string resultsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results");
        List<PendingToPublishItem> processed = [];

        ITextTransformer transformer = textTransformerBuilder.UseSpacesCleaner().Build();
        
        foreach (PendingToPublishItem item in items)
        {
            PendingToPublishItemSnapshot snapshot = item.GetSnapshot();
            string content = $"""
                              {snapshot.Title}
                              {string.Join(" ", snapshot.DescriptionList)}
                              {string.Join(" ", snapshot.Characteristics)}
                              {snapshot.Address}
                              {snapshot.Price.ToString()} {snapshot.IsNds.ToString()}
                              """;
            
            string transformed = transformer.TransformText(content);

            Directory.CreateDirectory(resultsDirectory);
            string filePath = Path.Combine(resultsDirectory, $"{Guid.NewGuid()}.txt");
            Result result = Result.CreateTextFile(transformed, filePath);
            await result.Publish();
            logger.Information("Published result: {Content}", content);
            PendingToPublishItem processedItem = item.MarkProcessed();
            processed.Add(processedItem);
        }

        await pendingItemsStorage.UpdateMany(processed);
        if (!await adapter.TransactionCommited(ct))
            logger.Error("Error at transaction commit.");
    }
}