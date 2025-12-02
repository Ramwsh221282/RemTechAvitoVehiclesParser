using Microsoft.Extensions.DependencyInjection;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

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
        await register.Handle(command);
        await Task.Delay(TimeSpan.FromSeconds(10));
        Assert.NotEmpty(TestParserRegistrationTicketApprovalService.Messages);
        bool hasSentTickets = await HasSentTickets();
        bool hasNotSentTickets = await HasNotSentTickets();
        Assert.True(hasSentTickets);
        Assert.False(hasNotSentTickets);
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
}