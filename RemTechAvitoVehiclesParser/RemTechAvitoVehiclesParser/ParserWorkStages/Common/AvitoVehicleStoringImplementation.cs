using System.Data;
using System.Text.Json;
using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common;

public static class AvitoVehicleStoringImplementation
{
    extension(AvitoVehicle)
    {
        public static async Task<AvitoVehicle[]> GetAsConcreteRepresentation(
            NpgSqlSession session,
            AvitoItemQuery query,
            CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = WhereClause(query);
            string limitClause = LimitClause(query);
            string lockClause = LockClause(query);
            
            string sql =
                $"""
                 SELECT
                 id as id,
                 url as url,
                 was_processed as was_processed,
                 retry_count as retry_count,
                 price as price,
                 is_nds as is_nds,
                 address as address,
                 photos as photos,
                 title as title,
                 characteristics as characteristics
                 FROM avito_parser_module.items
                 {filterSql}
                 {lockClause}
                 {limitClause}
                 """;
            
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<AvitoVehicle> result = [];
            while (reader.Read()) result.Add(AsConcreteItem(reader));
            return [.. result];
        }
        
        public static async Task<AvitoVehicle[]> GetAsCatalogueRepresentation(
            NpgSqlSession session,
            AvitoItemQuery query,
            CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = WhereClause(query);
            string limitClause = LimitClause(query);
            string lockClause = LockClause(query);
            
            string sql =
            $"""
            SELECT
            id as id,
            url as url,
            was_processed as was_processed,
            retry_count as retry_count,
            price as price,
            is_nds as is_nds,
            address as address,
            photos as photos
            FROM avito_parser_module.items
            {filterSql}
            {lockClause}
            {limitClause}
            """;
            
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<AvitoVehicle> result = [];
            while (reader.Read()) result.Add(AsCatalogueItem(reader));
            return [.. result];
        }
    }

    extension(IEnumerable<AvitoVehicle> items)
    {
        public async Task PersistAsCatalogueRepresentation(NpgSqlSession session)
        {
            const string sql =
            """
            INSERT INTO avito_parser_module.items
            (id, url, was_processed, retry_count, price, is_nds, address, photos)
            VALUES
            (@id, @url, @was_processed, @retry_count, @price, @is_nds, @address, @photos::jsonb)
            ON CONFLICT (id) DO NOTHING;
            """;
            IEnumerable<object> parameters = items.Select(ExtractParameters);
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task Remove(NpgSqlSession session)
        {
            const string sql = "DELETE FROM avito_parser_module.items WHERE id = @id";
            IEnumerable<object> parameters = items.Select(i => new { id = i.CatalogueRepresentation.Id });
            await session.ExecuteBulk(sql, parameters);
        }
        
        public async Task UpdateFull(NpgSqlSession session)
        {
            const string sql =
                """
                UPDATE avito_parser_module.items
                SET title = @title,
                    was_processed = @was_processed,
                    retry_count = @retry_count,
                    characteristics = @characteristics::jsonb
                WHERE id = @id;
                """;
            
            IEnumerable<object> parameters = items.Select(ResolveParameterByConcreteItemInitialization);
            await session.ExecuteBulk(sql, parameters);
        }
    }

    private static (DynamicParameters parameters, string filterSql) WhereClause(AvitoItemQuery query)
    {
        List<string> filters = [];
        DynamicParameters parameters = new();

        if (query.CatalogueOnly)
        {
            filters.Add("title is NULL");
            filters.Add("characteristics is NULL");   
        }

        if (query.ConcreteOnly)
        {
            filters.Add("title is NOT NULL");
            filters.Add("characteristics is NOT NULL");
        }
        
        if (query.UnprocessedOnly) filters.Add("was_processed is FALSE");
        if (query.RetryCount.HasValue)
        {
            filters.Add("retry_count <= @retry");
            parameters.Add("@retry", query.RetryCount.Value, DbType.Int32);
        }

        return (parameters, filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : "");
    }

    private static string LockClause(AvitoItemQuery query) => query.WithLock ? "FOR UPDATE" : "";
    private static string LimitClause(AvitoItemQuery query) => query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : "";
    
    private static object ExtractParameters(AvitoVehicle item) => new
    {
        id = item.CatalogueRepresentation.Id,
        url = item.CatalogueRepresentation.Url,
        was_processed = item.Processed,
        retry_count = item.RetryCount,
        price = item.CatalogueRepresentation.Price,
        is_nds = item.CatalogueRepresentation.IsNds,
        address = item.CatalogueRepresentation.Address,
        photos = JsonSerializer.Serialize(item.CatalogueRepresentation.Photos)
    };

    private static object ResolveParameterByConcreteItemInitialization(AvitoVehicle item)
    {
        if (!item.ConcretePageRepresentation.IsEmpty())
            return new
            {
                was_processed = item.Processed,
                retry_count = item.RetryCount,
                id = item.CatalogueRepresentation.Id,
                title = item.ConcretePageRepresentation.Title,
                characteristics = JsonSerializer.Serialize(item.ConcretePageRepresentation.Characteristics)
            };
        return new
        {
            was_processed = item.Processed,
            retry_count = item.RetryCount,
            id = item.CatalogueRepresentation.Id,
            title = (string?)null,
            characteristics = (string?)null
        };
    }
    
    private static AvitoVehicle AsCatalogueItem(IDataReader reader)
    {
        AvitoVehicleCatalogueRepresentation catalogue = new(
            Id: reader.GetString(reader.GetOrdinal("id")),
            Url: reader.GetString(reader.GetOrdinal("url")),
            Price: reader.GetInt64(reader.GetOrdinal("price")),
            IsNds: reader.GetBoolean(reader.GetOrdinal("is_nds")),
            Address: reader.GetString(reader.GetOrdinal("address")),
            Photos: JsonSerializer.Deserialize<string[]>(reader.GetString(reader.GetOrdinal("photos")))!
        );
        int retryCount = reader.GetInt32(reader.GetOrdinal("retry_count"));
        bool processed = reader.GetBoolean(reader.GetOrdinal("was_processed"));
        AvitoVehicle vehicle = AvitoVehicle.Create(processed, retryCount, catalogue, AvitoVehicleConcretePageRepresentation.Empty());
        return vehicle;
    }
    
    private static AvitoVehicle AsConcreteItem(IDataReader reader)
    {
        AvitoVehicle catalogue = AsCatalogueItem(reader);
        string title = reader.GetString(reader.GetOrdinal("title"));
        string characteristicsJson = reader.GetString(reader.GetOrdinal("characteristics"));
        Dictionary<string, string> characteristics = JsonSerializer.Deserialize<Dictionary<string, string>>(characteristicsJson)!;
        AvitoVehicleConcretePageRepresentation concrete = new(title, characteristics);
        return AvitoVehicle.Create(catalogue.Processed, catalogue.RetryCount, catalogue.CatalogueRepresentation, concrete);
    }
}
