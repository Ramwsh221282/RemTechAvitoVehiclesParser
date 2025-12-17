using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.PaginationParsing.Extensions;

public static class ProcessingParsingParserLinkConstruction
{
    extension(ProcessingParserLink)
    {
        public static ProcessingParserLink[] FromDeliverEventArgs(BasicDeliverEventArgs ea)
        {
            byte[] body = ea.Body.ToArray();
            string json = Encoding.UTF8.GetString(body);
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement rootElement = document.RootElement.GetProperty("parser_links");
            ProcessingParserLink[] links = new ProcessingParserLink[rootElement.GetArrayLength()];
            int index = 0;
            foreach (var element in rootElement.EnumerateArray())
            {
                Guid id = element.GetProperty("id").GetGuid();
                string url = element.GetProperty("url").GetString()!;
                ProcessingParserLink link = new(id, url, false, 0);
                links[index] = link;
                index++;
            }
            return links;
        }
    }
}
