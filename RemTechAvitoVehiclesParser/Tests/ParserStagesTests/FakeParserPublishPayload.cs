using System.Text;
using System.Text.Json;

namespace Tests.ParserStagesTests;

public sealed record FakeParserPublishPayload(FakeParserData Parser, IReadOnlyList<FakeParserLinkData> Links)
{
    public ReadOnlyMemory<byte> GetPayloadForRabbitMq()
    {
        object body = new
        {
            parser_id = Parser.Id,
            parser_domain = Parser.Domain,
            parser_type = Parser.Type,
            parser_links = Links.Select(l => new
            {
                id = l.Id,
                url = l.Url
            })
        };
        
        string json = JsonSerializer.Serialize(body);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(json);
        return new ReadOnlyMemory<byte>(bodyBytes);
    }
}