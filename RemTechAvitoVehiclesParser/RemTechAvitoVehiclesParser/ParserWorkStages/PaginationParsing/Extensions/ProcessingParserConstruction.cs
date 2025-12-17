using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.PaginationParsing.Extensions;

public static class PaginationParsingParserConstruction
{
    extension(ProcessingParser)
    {
        public static ProcessingParser FromDeliverEventArgs(BasicDeliverEventArgs ea)
        {
            byte[] body = ea.Body.ToArray();
            string json = Encoding.UTF8.GetString(body);
            using JsonDocument document = JsonDocument.Parse(json);
            Guid id = document.RootElement.GetProperty("parser_id").GetGuid();
            string type = document.RootElement.GetProperty("parser_type").GetString()!;
            string domain = document.RootElement.GetProperty("parser_domain").GetString()!;
            return new ProcessingParser(id, type, domain);
        }
    }
}
