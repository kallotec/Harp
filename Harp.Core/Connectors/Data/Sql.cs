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
        string getConnectionString() => "Server=.\\SQLEXPRESS;Database=Harp;Integrated Security=SSPI;";


        public string[] GetColumnNames(EntityType entity)
        {
            var tableObjectId = GetTableObjectId(entity);

            var command = string.Format("select name from sys.columns where object_id = '{0}'", tableObjectId);
            var results = QuerySingleColumn(command);

            return results.ToArray();
        }

        public string GetTableObjectId(EntityType entity)
        {
            var command = "select name, [object_id] from sys.tables where type = 'U'";
            var results = QueryDoubleColumn(command);

            var entityName = entity.ToString();

            var match = results.SingleOrDefault(r => StringMatcher.IsAFuzzyMatch(r.first, entityName));

            return match.second;
        }

        public string[] GetAllStoredProcNames()
        {
            var command = "select * from sys.procedures where [type] = 'P'";
            var results = QuerySingleColumn(command);

            return results.ToArray();
        }

        public List<(string first, string second)> QueryDoubleColumn(string sql)
        {
            using (var conn = new SqlConnection(getConnectionString()))
            {
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = sql;
                    cmd.CommandType = System.Data.CommandType.Text;
                    var reader = cmd.ExecuteReader();

                    var values = new List<(string first, string second)>();

                    while (reader.Read())
                        values.Add((reader.GetString(0), reader.GetString(1)));

                    return values;
                }
            }
        }

        public List<string> QuerySingleColumn(string sql)
        {
            using (var conn = new SqlConnection(getConnectionString()))
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
            using (var conn = new SqlConnection(getConnectionString()))
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
            using (var conn = new SqlConnection(getConnectionString()))
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

    }
}
