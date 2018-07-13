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
        public GenerateResult SynchronizeFileWithDb(HarpFile mapFile, string sqlConnectionString, string outputFolder, out StringBuilder trace)
        {
            trace = new StringBuilder();

            try
            {
                var sql = new Sql(sqlConnectionString);

                //var firstItem = entitiesAndBehaviors.First();

                //var tableId = sql.GetTableObjectId(firstItem.Key);
                //var tableName = sql.GetTableName(tableId);
                //trace.AppendLine($"Sql object match: {tableName} ({tableId})");

                //var columnNames = sql.GetColumnNames(tableId);
                //trace.AppendLine($"Columns:");
                //foreach (var name in columnNames)
                //    trace.AppendLine(" - " + name);

                //// generate entity class
                //var classDefinition = generateEntityClass(tableName, rootNamespace, columnNames);
                //var className = translateTableNameToClassName(tableName);

                //var outputFile = Path.Combine(outputFolder, className + ".cs");
                //File.WriteAllText(outputFile, classDefinition);

                return GenerateResult.OK;
            }
            catch (Exception ex)
            {
                trace.AppendLine($"{ex.GetType().Name}: {ex.ToString()}");
                return GenerateResult.UnknownError;
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

        public enum GenerateResult { UnknownError, OK }

    }

}
