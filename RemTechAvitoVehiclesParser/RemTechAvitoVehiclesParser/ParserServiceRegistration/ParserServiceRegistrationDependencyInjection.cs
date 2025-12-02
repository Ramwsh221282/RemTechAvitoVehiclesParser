using RemTechAvitoVehiclesParser.ParserServiceRegistration.BackgroundTasks;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.ConfirmPendingCreationTicket;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.ConfirmPendingCreationTicket.Decorators;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket.Decorators;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.RabbitMq;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration;

public static class ParserServiceRegistrationDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserServiceRegistrationContext()
        {
            services.AddRegisterParserCreationTicketCommand();
            services.AddConfirmParserCreationTicketCommand();
            services.RegisterNpgSqlStorage();
            services.RegisterRegisteredTicketRabbitMqPublisher();
            services.RegisterPublishRegistrationTicketsJob();
            services.AddConfirmParserCreationTicketService();
        }
        
        private void AddRegisterParserCreationTicketCommand()
        {
            services.AddScoped<IRegisterParserCreationTicket, RegisterParserCreationTicket>();
            services.Decorate<IRegisterParserCreationTicket, RegisterParserCreationTicketTransaction>();
            services.Decorate<IRegisterParserCreationTicket, RegisterParserCreationTicketLogging>();
        }

        private void AddConfirmParserCreationTicketCommand()
        {
            services.AddScoped<IConfirmPendingCreationTicket, ConfirmPendingCreationTicket>();
            services.Decorate<IConfirmPendingCreationTicket, ConfirmPendingCreationTicketTransaction>();
            services.Decorate<IConfirmPendingCreationTicket, ConfirmPendingCreationTicketLogging>();
        }

        private void AddConfirmParserCreationTicketService()
        {
            services.AddHostedService<ConfirmPendingRegistrationTicketService>();
        }
        
        private void RegisterNpgSqlStorage()
        {
            services.AddScoped<NpgSqlRegisteredTicketsStorage>();
        }

        private void RegisterRegisteredTicketRabbitMqPublisher()
        {
            services.AddScoped<RegisterTicketRabbitMqPublisher>();
        }

        public void RegisterPublishRegistrationTicketsJob()
        {
            services.AddTransient<ICronScheduleJob, PublishPendingRegistrationTicketsToQueue>();
        }
    }

    extension(IServiceProvider serviceProvider)
    {
        public async Task RegisterParserCreationTicket(string domain, string type)
        {
            RegisterParserCreationTicketCommand command = new(domain, type);
            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
            IRegisterParserCreationTicket registration = scope.ServiceProvider.GetRequiredService<IRegisterParserCreationTicket>();
            await registration.Handle(command);
        }
    }
}