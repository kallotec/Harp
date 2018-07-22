using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Harp.Core.Models
{
    public class HarpFile
    {
        public HarpConfig Config { get; set; } = new HarpConfig();
        public List<Entity> Entities { get; set; } = new List<Entity>();
        [YamlIgnore]
        public bool IsFullyMapped => Entities.All(e => e.IsFullyMapped);

        public static HarpFile FromYaml(string harpYaml)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build();

                var harpFile = deserializer.Deserialize<HarpFile>(harpYaml);
                return harpFile;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public string GenerateYaml()
        {
            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build();

                var yaml = serializer.Serialize(this);
                return yaml;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }

    public class HarpConfig
    {
        public string SqlConnectionString { get; set; }
        public string OutputDirectory { get; set; }
        [YamlIgnore]
        public bool IsFullyMapped => (!string.IsNullOrWhiteSpace(SqlConnectionString) &&
                                     !string.IsNullOrWhiteSpace(SqlConnectionString));
    }
}
