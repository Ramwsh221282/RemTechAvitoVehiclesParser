using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace Tests.ParserServiceRegistrationTests;

public sealed class ParserServiceRegistrationTests(ParserServiceRegistrationFixture fixture) : IClassFixture<ParserServiceRegistrationFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;

    [Fact]
    private async Task Publish_Parser_Registration_Success()
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        IRegisterParserCreationTicket register = scope.ServiceProvider.GetRequiredService<IRegisterParserCreationTicket>();
        RegisterParserCreationTicketCommand command = new("test-parser-domain", "test-parser-type");
        RegisterParserServiceTicketSnapshot ticket = await register.Handle(command);
        await Task.Delay(TimeSpan.FromSeconds(10));
        Assert.NotEmpty(TestParserRegistrationTicketApprovalService.Messages);
        RegisterParserServiceTicketSnapshot registeredTicket = await GetRegisteredTicket(ticket.Id);
        using JsonDocument document = JsonDocument.Parse(registeredTicket.Payload);
        string type = document.RootElement.GetProperty("parser_type").GetString()!;
        string domain = document.RootElement.GetProperty("parser_domain").GetString()!;
        bool hasSentTickets = await HasSentTickets();
        bool hasNotSentTickets = await HasNotSentTickets();
        Assert.True(hasSentTickets);
        Assert.False(hasNotSentTickets);
        await PublishForConfirmation(ticket.Id, domain, type);
        await Task.Delay(TimeSpan.FromSeconds(20));
        bool hasNoTickets = await HasNoTicketsAtAll();
        Assert.True(hasNoTickets);
    }

    private async Task<RegisterParserServiceTicketSnapshot> GetRegisteredTicket(Guid id)
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        NpgSqlRegisteredTicketsStorage storage = scope.ServiceProvider.GetRequiredService<NpgSqlRegisteredTicketsStorage>();
        QueryRegisteredTicketArgs args = new(Id: id);
        Maybe<RegisterParserServiceTicket> ticket = await storage.GetTicket(args);
        return ticket.Value.GetSnapshot();
    }

    private async Task<bool> HasNoTicketsAtAll()
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        NpgSqlRegisteredTicketsStorage storage = scope.ServiceProvider.GetRequiredService<NpgSqlRegisteredTicketsStorage>();
        QueryRegisteredTicketArgs args = new();
        IEnumerable<RegisterParserServiceTicket> tickets = await storage.GetTickets(args);
        return tickets.Any() == false;
    }
    
    private async Task<bool> HasSentTickets()
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        NpgSqlRegisteredTicketsStorage storage = scope.ServiceProvider.GetRequiredService<NpgSqlRegisteredTicketsStorage>();
        QueryRegisteredTicketArgs args = new(SentOnly: true);
        IEnumerable<RegisterParserServiceTicket> tickets = await storage.GetTickets(args);
        return tickets.Any();
    }

    private async Task<bool> HasNotSentTickets()
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        NpgSqlRegisteredTicketsStorage storage = scope.ServiceProvider.GetRequiredService<NpgSqlRegisteredTicketsStorage>();
        QueryRegisteredTicketArgs args = new(NotSentOnly: true);
        IEnumerable<RegisterParserServiceTicket> tickets = await storage.GetTickets(args);
        return tickets.Any();
    }

    private async Task PublishForConfirmation(Guid id, string domain, string type)
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        PublisherToParserRegistrationTicketApproval publisher = scope.ServiceProvider.GetRequiredService<PublisherToParserRegistrationTicketApproval>();
        await publisher.Publish(id, domain, type);
    }
}