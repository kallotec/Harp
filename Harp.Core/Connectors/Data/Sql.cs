using Harp.Core.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using Dapper;

namespace Harp.Core.Connectors.Data
{
    /*
     * TODO: Use Dapper
     */

    public class Sql
    {
        public Sql(string connectionString)
        {
            this.connectionString = connectionString;
        }

        string connectionString;


        public int GetTableObjectId(string tableName)
        {
            var command = "select name, [object_id] from sys.tables where type = 'U'";
            var results = QueryDoubleColumn(command);

            var match = results.SingleOrDefault(r => StringMatcher.IsAFuzzyMatch(r.first, tableName));

            return match.second;
        }

        public string GetTableName(int objectId)
        {
            var command = $"select name from sys.tables where object_id = {objectId}";
            var result = QueryScalar(command);
            
            return result;
        }

        public string[] GetColumnNames(int tableObjectId)
        {
            var command = string.Format("select name from sys.columns where object_id = '{0}'", tableObjectId);
            var results = QuerySingleColumn(command);

            return results.ToArray();
        }

        public int[] GetStoredProcsThatRefEntity(int objectId)
        {
            var query = getQuery_ProcsThatReferenceObject(objectId);

            var results = QuerySingleColumn(query).Select(r => int.Parse(r));

            return results.ToArray();
        }

        public string GetFullObjectName(int objectId)
        {
            var query = $"SELECT OBJECT_SCHEMA_NAME({objectId}) + '.' + OBJECT_NAME({objectId}) as object_name";
            var result = QueryScalar(query);
            return result;
        }

        public string[] GetAllStoredProcNames()
        {
            var command = "select * from sys.procedures where [type] = 'P'";
            var results = QuerySingleColumn(command);

            return results.ToArray();
        }


        public List<(string first, int second)> QueryDoubleColumn(string sql)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;
                    var reader = cmd.ExecuteReader();

                    var values = new List<(string first, int second)>();

                    while (reader.Read())
                        values.Add((reader.GetString(0), reader.GetInt32(1)));

                    return values;
                }
            }
        }

        public List<string> QuerySingleColumn(string sql)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;
                    var reader = cmd.ExecuteReader();

                    var values = new List<string>();
                    var value = string.Empty;

                    while (reader.Read())
                        values.Add(reader.GetString(0));

                    return values;
                }
            }
        }

        public string QueryScalar(string sql)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;
                    var scalarValue = cmd.ExecuteScalar().ToString();
                    return scalarValue;
                }
            }
        }

        public int Execute(string sql)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;
                    var affectedRows = cmd.ExecuteNonQuery();
                    return affectedRows;
                }
            }
        }

        string getQuery_ProcsThatReferenceObject(int objectId)
        {
            var query = @"
    declare @objectId int = " + objectId + @";

    DECLARE @sps TABLE(name VARCHAR(255), objectType VARCHAR(3), objectId INT, schemaName VARCHAR(20), fullName VARCHAR(255))

    --get all user created sp's in db instance
    INSERT INTO @sps(name, objectType, objectId, schemaName, fullName)
    SELECT so.name, LTRIM(RTRIM(so.type)), so.object_id, OBJECT_SCHEMA_NAME(so.object_id), (OBJECT_SCHEMA_NAME(so.object_id) + '.' + so.name)
    FROM sys.objects so
    WHERE so.type = 'P' OR so.type = 'U' OR so.type = 'V' OR so.type = 'FN' OR so.type = 'IF' OR so.type = 'TF'

    --collate all references between only the above db objects
    DECLARE @references TABLE(
        [fromId] int,
        [fromType] VARCHAR(3),
        [toId] int,
        [toType] VARCHAR(3)
    )

    INSERT INTO
        @references([fromId], [fromType], [toId], [toType])
    SELECT
        d.referencing_id,
        sp.objectType,
        sp2.objectId,
        sp2.objectType
    FROM
        sys.sql_expression_dependencies d
    INNER JOIN
        @sps sp ON sp.objectId = d.referencing_id--enforce that it is a user created SP
    INNER JOIN
        @sps sp2 ON sp2.name = d.referenced_entity_name--enforce that it is a user created SP
    WHERE
        d.referenced_class = 1
        AND d.referencing_class = 1

    SELECT
        r.[fromId] as object_id
        --OBJECT_SCHEMA_NAME(r.[fromId]) + '.' + OBJECT_NAME(r.[fromId]) as object_name
        /*OBJECT_SCHEMA_NAME(r.[fromId]) as [fromSchema],
        r.[fromId],
        OBJECT_NAME(r.[fromId]) as [from],
        r.[fromType],
        OBJECT_SCHEMA_NAME(r.[toId]) as [toSchema],
        r.[toId],
        OBJECT_NAME(r.[toId]) as [to],
        r.[toType]*/

    FROM
        @references r
    WHERE
        r.toId = @objectId
        and r.fromType = 'P'";

            return query;
        }

    }
}
