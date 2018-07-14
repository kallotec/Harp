using Harp.Core.Utilities;
using Harp.Core.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using Humanizer;

namespace Harp.Core.Services
{
    public class HarpSynchronizer
    {
        public SynchronizeResult SynchronizeFile(HarpFile mapFile, string sqlConnectionString, out StringBuilder trace)
        {
            trace = new StringBuilder();

            try
            {
                var sql = new Sql(sqlConnectionString);

                foreach (var entity in mapFile.Entities)
                {
                    int? tableId;

                    // Table
                    if (string.IsNullOrWhiteSpace(entity.TableName))
                    {
                        // Try match to an existing table
                        var tables = sql.GetAllTables();

                        var matches = tables.Where(
                            t => StringMatcher.IsAFuzzyMatch(
                                getObjectName(t.fullName), entity.EntityName
                            ));

                        if (matches.Count() > 1)
                        {
                            // Error: too many matches, can't decide.
                            return SynchronizeResult.EntityNameMatchesTooManyTables;
                        }
                        else if (matches.Count() == 0)
                        {
                            // Error: no matches
                            return SynchronizeResult.EntityNameMatchesNoTables;
                        }

                        var tableMatch = matches.Single();

                        // Capture table name on entity
                        entity.TableName = tableMatch.fullName;

                    }

                    tableId = sql.GetTableObjectId(entity.TableName);
                    if (tableId == null)
                        return SynchronizeResult.MatchedTableDoesNotExist;

                    trace.AppendLine($"Table match: {entity.TableName} ({tableId})");

                    // Columns
                    var columnNames = sql.GetColumnNames(tableId.Value);

                    trace.AppendLine($"Columns:");
                    foreach (var name in columnNames)
                        trace.AppendLine(" - " + name);

                    foreach (var prop in entity.Properties.Where(p => !p.IsMapped))
                    {
                        var colMatches = columnNames.Where(c => StringMatcher.IsAFuzzyMatch(c, prop.Name));

                        if (colMatches.Count() == 1)
                        {
                            prop.ColumnName = colMatches.Single();
                        }
                        else
                        {
                            // Error: Too matches for column
                            // Error: No matches for column
                            return SynchronizeResult.ColumnMatchingError;
                        }

                    }

                }

                return SynchronizeResult.OK;

                // TODO: Uncomment when behaviours are syncing correctly
                //return mapFile.Entities.All(e => e.IsFullyMapped) 
                //    ? SynchronizeResult.OK 
                //    : SynchronizeResult.UnknownError;
            }
            catch (Exception ex)
            {
                trace.AppendLine($"{ex.GetType().Name}: {ex.ToString()}");
                return SynchronizeResult.UnknownError;
            }

        }

        string generateEntityClass(string tableName, string rootNamespace, string[] columnNames)
        {
            var className = translateTableNameToClassName(tableName);

            var symbolProps = "{{{properties}}}";
            var template = @"
using System;
namespace " + rootNamespace + @"
{
    public class " + className + @"
    {
        " + symbolProps + @"
    }
}
";
            var propertyNames = columnNames.Select(translateColumnToPropName);
            var propertyDefinitions = propertyNames.Select(translateNameToPropDefinition);

            var classDefinition = template.Replace(symbolProps, string.Join(Environment.NewLine + "\t\t", propertyDefinitions));

            return classDefinition;
        }

        string translateTableNameToClassName(string tableName)
        {
            return tableName.Humanize(LetterCasing.Title).Singularize().Dehumanize();
        }

        string translateColumnToPropName(string columnName)
        {
            return columnName.Humanize(LetterCasing.Title).Dehumanize();
        }

        string translateNameToPropDefinition(string propertyName)
        {
            return "public string " + propertyName + " { get; set; }";
        }

        string getObjectName(string fullTableName)
        {
            if (!fullTableName.Contains("."))
                return fullTableName;

            var components = fullTableName.Split(".", StringSplitOptions.RemoveEmptyEntries);
            return components.Last();
        }

        public enum SynchronizeResult
        {
            UnknownError,
            OK,
            EntityNameMatchesTooManyTables,
            EntityNameMatchesNoTables,
            MatchedTableDoesNotExist,
            ColumnMatchingError,
        }

    }

}
