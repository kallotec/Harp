using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;
using System.IO;
using Harp.Core.Models;

namespace Harp.Core.Services
{
    public class HarpFileReader
    {
        public (ReadResult code, HarpFile map) Read(string filePath, out StringBuilder trace)
        {
            trace = new StringBuilder();

            if (!File.Exists(filePath))
                return (ReadResult.CouldNotFindFile, null);

            var mapFile = parseHarpFile(filePath);
            if (mapFile == null)
                return (ReadResult.InvalidFileFormat, null);

            return (ReadResult.OK, mapFile);
        }

        HarpFile parseHarpFile(string harpFilePath)
        {
            var mapFile = new HarpFile();

            // get raw yaml nodes
            var nodes = getNodes(harpFilePath);
            if (nodes == null)
                return null;

            // parse into MapFile instance
            foreach (var node in nodes)
            {
                var entity = parseNode(node);
                mapFile.Entities.Add(entity);
            }

            return mapFile;
        }

        Entity parseNode(KeyValuePair<YamlNode, YamlNode> node)
        {
            try
            {
                var entity = new Entity();

                var keyNode = (node.Key as YamlScalarNode);
                entity.EntityName = keyNode.Value;

                var rootNode = (node.Value as YamlMappingNode);

                var tableNameNode = rootNode.Children.SingleOrDefault(c => (c.Key as YamlScalarNode).Value == "Table");
                entity.TableName = (tableNameNode.Value as YamlScalarNode).Value;

                var propNodes = (rootNode.Children.SingleOrDefault(c => (c.Key as YamlScalarNode).Value == "Properties").Value as YamlSequenceNode);

                foreach (var item in propNodes)
                {
                    string name = null;
                    string columnName = null;

                    if (item is YamlScalarNode)
                    {
                        name = (item as YamlScalarNode).Value;
                    }
                    else if (item is YamlMappingNode)
                    {
                        var mappingNode = (item as YamlMappingNode).SingleOrDefault();

                        name = (mappingNode.Key as YamlScalarNode).Value;
                        columnName = (mappingNode.Value as YamlScalarNode).Value;
                    }
                    else
                    {
                        // invalid node type
                        return null;
                    }

                    entity.Properties.Add(new Property
                    {
                        Name = name,
                        ColumnName = columnName
                    });
                }

                var behavNodes = (rootNode.Children.SingleOrDefault(c => (c.Key as YamlScalarNode).Value == "Behaviors").Value as YamlSequenceNode);

                foreach (var item in behavNodes)
                {
                    string name = null;
                    string storedProcName = null;

                    if (item is YamlScalarNode)
                    {
                        name = (item as YamlScalarNode).Value;
                    }
                    else if (item is YamlMappingNode)
                    {
                        var mappingNode = (item as YamlMappingNode).SingleOrDefault();

                        name = (mappingNode.Key as YamlScalarNode).Value;
                        storedProcName = (mappingNode.Value as YamlScalarNode).Value;
                    }
                    else
                    {
                        // invalid node type
                        return null;
                    }

                    entity.Behaviors.Add(new Behavior
                    {
                        Name = name,
                        StoredProcName = storedProcName
                    });
                }

                return entity;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        IDictionary<YamlNode, YamlNode> getNodes(string harpFilePath)
        {
            try
            {
                var harpFileContents = File.ReadAllText(harpFilePath);
                var input = new StringReader(harpFileContents);

                var yaml = new YamlStream();
                yaml.Load(input);

                var rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;

                return rootNode.Children;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public enum ReadResult { UnknownError, OK, CouldNotFindFile, InvalidFileFormat }

    }
}
