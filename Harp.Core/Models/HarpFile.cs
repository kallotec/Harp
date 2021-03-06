﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.RepresentationModel;
using System.IO;

namespace Harp.Core.Models
{
    public class HarpFile
    {
        public HarpConfig Config { get; set; } = new HarpConfig();

        public Dictionary<string, Entity> Entities { get; set; } = new Dictionary<string, Entity>();

        [YamlIgnore]
        public bool IsFullyMapped => (Config.IsFullyMapped && Entities.All(e => e.Value.IsFullyMapped));

        public static (ParseResult code, HarpFile file) FromYaml(string harpYaml)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new PascalCaseNamingConvention())
                    .Build();

                var harpFile = deserializer.Deserialize<HarpFile>(harpYaml);

                if (harpFile.Entities == null)
                    harpFile.Entities = new Dictionary<string, Entity>();

                // Attach entity names
                for (int x = 0; x < harpFile.Entities.Count; x++)
                {
                    var pair = harpFile.Entities.ElementAt(x);
                    var entity = pair.Value;

                    if (entity == null)
                    {
                        entity = new Entity();
                        harpFile.Entities[pair.Key] = entity;
                    }

                    // Attach entity name
                    entity.Name = pair.Key;

                    if (entity.Properties == null)
                        entity.Properties = new Dictionary<string, string>();

                    if (entity.Behaviors == null)
                        entity.Behaviors = new Dictionary<string, string>();
                }

                return (ParseResult.OK, harpFile);
            }
            catch (Exception ex)
            {
                return (ParseResult.InvalidFileFormat, null);
            }
        }

        public string GenerateYaml()
        {
            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(new PascalCaseNamingConvention())
                    .Build();

                var yaml = serializer.Serialize(this);
                return yaml;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public enum ParseResult { UnknownError, OK, InvalidFileFormat }

        public class HarpConfig
        {
            public string SqlConnectionString { get; set; }
            public string OutputDirectory { get; set; }
            [YamlIgnore]
            public bool IsFullyMapped => (!string.IsNullOrWhiteSpace(SqlConnectionString) &&
                                         !string.IsNullOrWhiteSpace(OutputDirectory));
        }

        //static IDictionary<YamlNode, YamlNode> getYamlNodes(string harpYaml)
        //{
        //    try
        //    {
        //        var input = new StringReader(harpYaml);

        //        var yaml = new YamlStream();
        //        yaml.Load(input);

        //        var rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;

        //        return rootNode.Children;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        //static Entity parseEntityNode(KeyValuePair<YamlNode, YamlNode> node)
        //{
        //    try
        //    {
        //        var entity = new Entity();

        //        var keyNode = (node.Key as YamlScalarNode);
        //        entity.Name = keyNode.Value;

        //        var rootNode = (node.Value as YamlMappingNode);

        //        // If only the entity name was specified then it's valid,
        //        // we will try autopopulate the remaining info via the synchronize step.
        //        if (rootNode == null)
        //            return entity;

        //        var tableNameNode = rootNode.SingleOrDefault(c => (c.Key as YamlScalarNode).Value == nameof(Entity.Table));
        //        entity.Table = (tableNameNode.Value as YamlScalarNode).Value;

        //        var propNodes = (rootNode.SingleOrDefault(c => (c.Key as YamlScalarNode).Value == nameof(Entity.Properties)).Value as YamlSequenceNode);

        //        foreach (var item in propNodes)
        //        {
        //            string name = null;
        //            string columnName = null;

        //            if (item is YamlScalarNode)
        //            {
        //                name = (item as YamlScalarNode).Value;
        //            }
        //            else if (item is YamlMappingNode)
        //            {
        //                var mappingNode = (item as YamlMappingNode).SingleOrDefault();

        //                name = (mappingNode.Key as YamlScalarNode).Value;
        //                columnName = (mappingNode.Value as YamlScalarNode).Value;
        //            }
        //            else
        //            {
        //                // invalid node type
        //                return null;
        //            }

        //            entity.Properties.Add(new Property
        //            {
        //                Name = name,
        //                Column = columnName
        //            });
        //        }

        //        var behavNodes = (rootNode.SingleOrDefault(c => (c.Key as YamlScalarNode).Value == nameof(Entity.Behaviors)).Value as YamlSequenceNode);

        //        foreach (var item in behavNodes)
        //        {
        //            string name = null;
        //            string storedProcName = null;

        //            if (item is YamlScalarNode)
        //            {
        //                name = (item as YamlScalarNode).Value;
        //            }
        //            else if (item is YamlMappingNode)
        //            {
        //                var mappingNode = (item as YamlMappingNode).SingleOrDefault();

        //                name = (mappingNode.Key as YamlScalarNode).Value;
        //                storedProcName = (mappingNode.Value as YamlScalarNode).Value;
        //            }
        //            else
        //            {
        //                // invalid node type
        //                return null;
        //            }

        //            entity.Behaviors.Add(new Behavior
        //            {
        //                Name = name,
        //                Proc = storedProcName
        //            });
        //        }

        //        return entity;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        /*
        var harpFile = new HarpFile();

        // get yaml nodes
        var nodes = getYamlNodes(harpYaml);
        if (nodes == null)
            return (ParseResult.InvalidFileFormat, null);

        // parse config
        var configNodeKey = nodes.FirstOrDefault((KeyValuePair<YamlNode, YamlNode> n) => (n.Key as YamlScalarNode).Value == nameof(harpFile.Config)).Key;
        var configNodeValue = nodes[configNodeKey] as YamlMappingNode;

        harpFile.Config.SqlConnectionString = (configNodeValue.FirstOrDefault(n => (n.Key as YamlScalarNode).Value == nameof(HarpFile.HarpConfig.SqlConnectionString))
                                                .Value as YamlScalarNode)
                                                .Value;
        harpFile.Config.OutputDirectory = (configNodeValue.FirstOrDefault(n => (n.Key as YamlScalarNode).Value == nameof(HarpFile.HarpConfig.OutputDirectory))
                                            .Value as YamlScalarNode)
                                            .Value;
        // trim off the config element
        nodes.Remove(configNodeKey);

        // parse entities
        foreach (var node in nodes)
        {
            var entity = parseEntityNode(node);

            if (entity != null)
                harpFile.Entities.Add(entity);
            else
                throw new InvalidOperationException("Entity was not properly formatted");

        }

        return (ParseResult.OK, harpFile);
        */
    }

}
