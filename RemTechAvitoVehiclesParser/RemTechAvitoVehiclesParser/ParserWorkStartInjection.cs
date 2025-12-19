using ParsingSDK.ParserInvokingContext;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.RabbitMq;
using RemTechAvitoVehiclesParser.ParserWorkStages.PaginationParsing;
using RemTechAvitoVehiclesParser.ParserWorkStages.PaginationParsing.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.Parsing.BackgroundTasks;

public static class ParserWorkStartInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterAvitoStartQueue()
        {
            services.RegisterParserInvokingQueue(RabbitMqProviding);
            services.RegisterAvitoStartQueueOptions();
            services.RegisterAvitoStartQueueHandle();
        }
        
        private void RegisterAvitoStartQueueOptions()
        {
            services.AddOptions<ParserStartOptions>().BindConfiguration(nameof(ParserStartOptions));
        }
        
        private void RegisterAvitoStartQueueHandle()
        {
            services.AddSingleton<ParserStartQueueHandle>(sp =>
            {
                NpgSqlConnectionFactory npgSql = sp.GetRequiredService<NpgSqlConnectionFactory>();
                Serilog.ILogger logger = sp.GetRequiredService<Serilog.ILogger>();
                return async (ea) =>
                {
                    await using NpgSqlSession session = new(npgSql);
                    await session.UseTransaction();
                    if (await ProcessingParser.HasAny(session))
                    {
                        logger.Information("There is already a parser in progress. Declining.");
                        return;
                    }

                    ProcessingParser parser = ProcessingParser.FromDeliverEventArgs(ea);
                    ProcessingParserLink[] links = ProcessingParserLink.FromDeliverEventArgs(ea);
                    ParserWorkStage stage = ParserWorkStage.PaginationStage();
                    await stage.Persist(session);
                    await parser.Persist(session);
                    await links.PersistMany(session);
                    await session.UnsafeCommit(CancellationToken.None);

                    logger.Information(
                        """
                        Added parser to process:
                        Domain: {domain}
                        Type: {type}
                        Id: {id}
                        Stage: {Stage}
                        Links: {Count}
                        """,
                        parser.Domain,
                        parser.Type,
                        parser.Id,
                        stage.Name,
                        links.Length
                    );
                };
            });
        }
    }
    
    private static Func<IServiceProvider, ParserStartQueueRabbitMqProvide> RabbitMqProviding => sp =>
    {
        RabbitMqConnectionSource connectionSource = sp.GetRequiredService<RabbitMqConnectionSource>();
        return async (ct) => await connectionSource.GetConnection(ct);
    };
}