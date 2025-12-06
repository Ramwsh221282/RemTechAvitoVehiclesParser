using System.Data;
using Dapper;
using ParsingSDK.Parsing;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Database;

public static class NpgSqlCataloguePageUrlConverting
{
    extension(NpgSqlCataloguePageUrlRow row)
    {
        public CataloguePageUrl ToModel() => CataloguePageUrl.MapFrom
        (
            row,
            r => r.Id,
            r => r.LinkId,
            r => r.Url,
            r => r.WasProcessed,
            r => r.RetryCount
        );
    }

    extension(NpgSqlCataloguePageUrlRow? row)
    {
        public Maybe<CataloguePageUrl> MaybeUrl() => row == null
            ? Maybe<CataloguePageUrl>.None()
            : Maybe<CataloguePageUrl>.Some(row.ToModel());
    }

    extension(IEnumerable<NpgSqlCataloguePageUrlRow> rows)
    {
        public IEnumerable<CataloguePageUrl> ToModels() => rows.Select(r => r.ToModel());
    }

    extension(CataloguePageItemQuery query)
    {
        public string LimitClause() => query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
    
        public string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
    
        public (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.Id.HasValue)
            {
                filters.Add("id=@id");
                parameters.Add("@id", query.Id.Value, DbType.Guid);
            }

            if (query.RetryLimitTreshold.HasValue)
            {
                filters.Add("retry_count < @retry_count");
                parameters.Add("@retry_count", query.RetryLimitTreshold.Value, DbType.Int32);
            }
            
            if (query.ProcessedOnly) filters.Add("was_processed is true");
            if (query.NotProcessedOnly) filters.Add("was_processed is false");
            
            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }
    }

    extension(IEnumerable<NpgSqlCataloguePageItemRow> rows)
    {
        public IEnumerable<CataloguePageItem> ToModels() => rows.Select(r => r.ToModel());
    }

    extension(NpgSqlCataloguePageItemRow row)
    {
        public CataloguePageItem ToModel() => CataloguePageItem.MapFrom
        (
            row,
            idMap: r => r.Id,
            catalogueIdMap: r => r.CatalogueUrlId,
            payloadMap: r => r.Payload,
            processedMap: r => r.WasProcessed,
            retryMap: r => r.RetryCount
        );
    }
    
    extension(CataloguePageItem item)
    {
        public object ExtractParameters() => new
        {
            id = item.Id,
            catalogue_url_id = item.CatalogueUrlId,
            was_processed = item.WasProcessed,
            retry_count = item.RetryCount,
            payload = item.Payload,
        };
    }
    
    extension(CataloguePageUrlQuery query)
    {
        public string LimitClause() => query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
    
        public string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
    
        public (DynamicParameters parameters, string filter) WhereClause()
        {
            List<string> filterSql = [];
            DynamicParameters parameters = new();

            if (query.LinkId.HasValue)
            {
                filterSql.Add("link_id = @link_id");
                parameters.Add("@link_id", query.LinkId.Value, DbType.Guid);
            }

            if (query.Id.HasValue)
            {
                filterSql.Add("id = @id");
                parameters.Add("@id", query.Id.Value, DbType.Guid);
            }

            if (query.RetryLimitTreshold.HasValue)
            {
                filterSql.Add("retry_count < @treshold");
                parameters.Add("@treshold", query.RetryLimitTreshold.Value, DbType.Int32);
            }
            
            if (query.ProcessedOnly.HasValue) filterSql.Add("was_processed is true");
            if (query.UnprocessedOnly.HasValue) filterSql.Add("was_processed is false");

            return filterSql.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filterSql));
        }
    }
    
    extension(CataloguePageUrl url)
    {
        public object ExtractParameters() => new
        {
            id = url.Id,
            link_id = url.LinkId,
            url = url.Url,
            was_processed = url.Processed,
            retry_count = url.RetryCount
        };
    }
}