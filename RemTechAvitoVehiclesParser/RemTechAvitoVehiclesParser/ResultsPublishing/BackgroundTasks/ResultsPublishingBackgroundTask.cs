using ParsingSDK.Parsing;
using ParsingSDK.TextProcessing;
using Quartz;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ResultsPublishing.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class ResultsPublishingBackgroundTask(
    NpgSqlConnectionFactory connectionFactory,
    Serilog.ILogger logger,
    TextTransformerBuilder textTransformerBuilder
    ) : ICronScheduleJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        await using NpgSqlSession adapter = new(connectionFactory);
        NpgSqlParserWorkStagesStorage stagesStorage = new(adapter);
        NpgSqlPendingToPublishItemsStorage pendingItemsStorage = new(adapter);

        await adapter.UseTransaction(ct);

        WorkStageQuery stageQuery = new(Name: WorkStageConstants.FinalizationStage, WithLock: true );
        Maybe<ParserWorkStage> stage = await stagesStorage.GetWorkStage(stageQuery, ct);
        if (!stage.HasValue) return;

        PendingItemsQuery itemsQuery = new(UnprocessedOnly: true, WithLock: true, Limit: 50);
        PendingToPublishItem[] items = (await pendingItemsStorage.GetMany(itemsQuery, ct)).ToArray();
        if (items.Length == 0)
        {
            ParserWorkStage sleeping = stage.Value.ChangeStage(new SleepingWorkStage(stage.Value));
            await stagesStorage.Update(sleeping, ct);
            await adapter.UnsafeCommit(ct);
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
                              {item.Price.ToString()} {item.IsNds.ToString()}
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
        if (!await adapter.Commited(ct))
            logger.Error("Error at transaction commit.");
    }
}