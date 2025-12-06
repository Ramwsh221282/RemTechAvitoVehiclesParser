using System.Data;
using Dapper;
using ParsingSDK.Parsing;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;

public static class NpgSqlParserWorkStageConverting
{
    extension(NpgSqlParserWorkStageRow row)
    {
        public ParserWorkStage ToModel() => ParserWorkStage.MapFrom
        (
            row,
            idMap: r => r.Id,
            nameMap: r => r.Name,
            createdMap: r => r.Created,
            finishMap: r => r.Finished
        );
    }

    extension(NpgSqlParserWorkStageRow? row)
    {
        public Maybe<ParserWorkStage> MaybeWorkStage() => row == null
            ? Maybe<ParserWorkStage>.None()
            : Maybe<ParserWorkStage>.Some(row.ToModel());
    }

    extension(IEnumerable<NpgSqlParserWorkStageRow> rows)
    {
        public IEnumerable<ParserWorkStage> ToModels() => rows.Select(r => r.ToModel());
    }
    
    extension(ParserWorkStage stage)
    {
        public object ExtractParameters() => new
        {
            id = stage.Id,
            name = stage.Name,
            created = stage.Created,
            finished = stage.Finished,
        };
    }

    extension(WorkStageQuery query)
    {
        public (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.Id.HasValue)
            {
                filters.Add("id=@id");
                parameters.Add("@id", query.Id.Value, DbType.Guid);
            }

            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                filters.Add("name=@name");
                parameters.Add("@name", query.Name, DbType.String);
            }

            if (query.OnlyFinished)
            {
                filters.Add("finished is not null");
            }

            if (query.OnlyNotFinished)
            {
                filters.Add("finished is not null");
            }

            string resultSql = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);
            return (parameters, resultSql);
        }
        
        public string LockClause()
        {
            return query.WithLock ? "FOR UPDATE" : string.Empty;
        }

        public string LimitClause()
        {
            return query.Limit.HasValue ? $"LIMIT {query.Limit}" : string.Empty;
        }
    }
}