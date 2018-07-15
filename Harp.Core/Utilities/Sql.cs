using Harp.Core.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using Dapper;

namespace Harp.Core.Utilities
{
    /*
     * TODO: Use Dapper
     */

    public class Sql : ISql
    {
        public Sql(string connectionString)
        {
            this.connectionString = connectionString;
        }

        string connectionString;


        public int? GetTableObjectId(string fullTableName)
        {
            var simpleTableName = getObjectName(fullTableName);

            var command = $"select [object_id] from sys.tables where type = 'U' and name = '{simpleTableName}'";
            var result = QueryScalar(command);

            if (string.IsNullOrWhiteSpace(result))
                return null;

            return int.Parse(result);
        }

        public List<(string fullName, int objectId)> GetAllTables()
        {
            var command = $"select object_schema_name(object_id) + '.' + [name], object_id from sys.tables where type = 'U'";
            var result = QueryDoubleColumn(command);

            return result;
        }

        public string[] GetColumnNames(int tableObjectId)
        {
            var command = string.Format("select name from sys.columns where object_id = '{0}'", tableObjectId);
            var results = QuerySingleColumn(command);

            return results.ToArray();
        }

        public List<(string fullName, int objectId)> GetStoredProcsThatRefEntity(string tableName)
        {
            var tables = GetAllTables();

            var objectId = tables.Single(t => string.Equals(getObjectName(t.fullName), 
                                                            getObjectName(tableName), 
                                                            StringComparison.OrdinalIgnoreCase)).objectId;

            var query = getQueryProcIdsThatReferenceObject(objectId);
            var results = QueryDoubleColumn(query);
            return results;
        }


        List<(string first, int second)> QueryDoubleColumn(string sql)
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

        List<string> QuerySingleColumn(string sql)
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

        string QueryScalar(string sql)
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

        int Execute(string sql)
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

        string getQueryProcIdsThatReferenceObject(int objectId)
        {
            var query = @"select (referencing_schema_name + '.' + referencing_entity_name), referencing_id from sys.dm_sql_referencing_entities (object_name(" + objectId + "), 'OBJECT')";
            return query;
        }

        string getObjectName(string fullTableName)
        {
            if (!fullTableName.Contains("."))
                return fullTableName;

            var components = fullTableName.Split(".", StringSplitOptions.RemoveEmptyEntries);
            return components.Last();
        }

    }

    public enum ProcAction { Select, SelectAll, InsertUpdate, Delete }

}
