using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel;
using Harp.Core.Connectors.Data;
using Harp.Core.Utilities;

namespace Harp.Core.Generators
{
    public class Generator
    {
        public GenerateResult Generate(string harpFilePath, string sqlConnectionString, string outputFolder, out StringBuilder trace)
        {
            trace = new StringBuilder();

            if (!File.Exists(harpFilePath))
                return GenerateResult.CouldNotFindHarpFile;

            var entitiesAndBehaviors = parseHarpFile(harpFilePath);
            if (entitiesAndBehaviors == null)
                return GenerateResult.InvalidHarpFileFormat;

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            try
            {
                var sql = new Sql(sqlConnectionString);

                var firstItem = entitiesAndBehaviors.First();

                var tableId = sql.GetTableObjectId(firstItem.Key);
                var tableName = sql.GetTableName(tableId);
                trace.AppendLine($"Sql object match: {tableName} ({tableId})");

                var columnNames = sql.GetColumnNames(tableId);
                trace.AppendLine($"Columns:");
                foreach (var name in columnNames)
                    trace.AppendLine(" - " + name);

                return GenerateResult.OK;
            }
            catch (Exception ex)
            {
                trace.AppendLine($"{ex.GetType().Name}: {ex.ToString()}");
                return GenerateResult.UnknownError;
            }

            return GenerateResult.UnknownError;
        }

        Dictionary<string, string[]> parseHarpFile(string harpFilePath)
        {
            try
            {
                var harpFileContents = File.ReadAllText(harpFilePath);
                var input = new StringReader(harpFileContents);

                var yaml = new YamlStream();
                yaml.Load(input);

                var rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;

                return rootNode.Children.ToDictionary(
                    r => ((YamlScalarNode)r.Key).Value,
                    r => ((YamlSequenceNode)r.Value).Children
                            .Select(c => ((YamlScalarNode)c).Value).ToArray());
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }

    public enum GenerateResult { UnknownError, OK, CouldNotFindHarpFile, InvalidHarpFileFormat }

}
