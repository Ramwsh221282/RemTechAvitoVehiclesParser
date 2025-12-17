using Microsoft.Extensions.Options;
using ParserSubscriber.SubscribtionContext;
using ParserSubscriber.SubscribtionContext.Options;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace RemTechAvitoVehiclesParser;

public static class ParserSubscriptionInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserSubscription()
        {
            services.AddOptions<RabbitMqRequestReplyResponseListeningQueueOptions>().BindConfiguration(nameof(RabbitMqRequestReplyResponseListeningQueueOptions));
            services.RegisterParserSubscriber<RabbitMqRequestReplySubscriber>(s =>
                {
                    s.AddSingleton<RabbitMqConnectionProvider>(sp =>
                    {
                        RabbitMqConnectionSource connectionSource = sp.GetRequiredService<RabbitMqConnectionSource>();
                        RabbitMqConnectionProvider provider = async (ct) => await connectionSource.GetConnection(ct);
                        return provider;
                    });
                },
                s => s.AddSingleton<NpgSqlProvider>(sp =>
                {
                    IOptions<NpgSqlOptions> options = sp.GetRequiredService<IOptions<NpgSqlOptions>>();
                    return new NpgSqlProvider() { ConnectionString = options.Value.ToConnectionString() };
                }),
                "avito_parser_module"
            );
        }
    }
}