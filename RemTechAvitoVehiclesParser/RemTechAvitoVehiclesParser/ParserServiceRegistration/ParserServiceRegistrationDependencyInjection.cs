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
            services.RegisterRemoveConfirmiedRegistrationTickets();
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

        public void RegisterRemoveConfirmiedRegistrationTickets()
        {
            services.AddSingleton<ICronScheduleJob, RemoveConfirmedRegistrationTicketsService>();
        }
        
        public void RegisterPublishRegistrationTicketsJob()
        {
            services.AddSingleton<ICronScheduleJob, PublishPendingRegistrationTicketsToQueue>();
        }
    }
}