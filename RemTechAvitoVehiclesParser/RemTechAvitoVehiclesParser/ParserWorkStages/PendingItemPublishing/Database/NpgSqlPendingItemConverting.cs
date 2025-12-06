using System.Text.Json;
using Dapper;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Database;

public static class NpgSqlPendingItemConverting
{
    extension(IEnumerable<NpgSqlPendingItemRow> rows)
    {
        public IEnumerable<PendingToPublishItem> ToModels() => rows.Select(r => r.ToModel());
    }
    
    extension(NpgSqlPendingItemRow row)
    {
        public PendingToPublishItem ToModel()
        {
            return new PendingToPublishItem(
                Id: row.Id,
                Url: row.Url,
                Title: row.Title,
                Address: row.Address,
                Price: row.Price,
                IsNds: row.IsNds,
                DescriptionList: row.DescriptionList.ReadStringJsonArray(),
                Characteristics: row.Characteristics.ReadStringJsonArray(),
                Photos: row.Photos.ReadStringJsonArray(),
                WasProcessed: row.WasProcessed);
        }

        private IReadOnlyList<string> ReadStringJsonArray(string jsonArray)
        {
            using JsonDocument document = JsonDocument.Parse(jsonArray);
            List<string> items = new List<string>(document.RootElement.GetArrayLength());
            foreach (JsonElement item in document.RootElement.EnumerateArray())
                items.Add(item.GetString()!);
            return items;
        }
    }

    extension(string input)
    {
        private IReadOnlyList<string> ReadStringJsonArray()
        {
            using JsonDocument document = JsonDocument.Parse(input);
            List<string> items = new List<string>(document.RootElement.GetArrayLength());
            foreach (JsonElement item in document.RootElement.EnumerateArray())
                items.Add(item.GetString()!);
            return items;
        }
    }

    extension(PendingItemsQuery query)
    {
        public string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
        public string LimitClause() => query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
    
        public (DynamicParameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();
            if (query.UnprocessedOnly) filters.Add("was_processed is FALSE");
            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }
    }

    extension(PendingToPublishItem item)
    {
        public object ExtractParameters() => new
        {
            id = item.Id,
            url = item.Url,
            title = item.Title,
            address = item.Address,
            price = item.Price,
            is_nds = item.IsNds,
            description_list = JsonSerializer.Serialize(item.DescriptionList),
            characteristics = JsonSerializer.Serialize(item.Characteristics),
            photos = JsonSerializer.Serialize(item.Photos),
            was_processed = item.WasProcessed
        };
    }
}