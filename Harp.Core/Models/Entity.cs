using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace Harp.Core.Models
{
    public class Entity
    {
        public string Name { get; set; }
        public string Table { get; set; }
        public List<Property> Properties { get; set; } = new List<Property>();
        public List<Behavior> Behaviors { get; set; } = new List<Behavior>();
        [YamlIgnore]
        public bool IsFullyMapped => (Properties.Any() && Properties.All(p => p.IsMapped) &&
                                      Behaviors.Any() && Behaviors.All(b => b.IsMapped));

        public string GenerateHarpFileFragment()
        {
            var props = Properties.Select(p => $"- {p.Name}: {p.Column}");
            var behavs = Behaviors.Select(b => $"- {b.Name}: {b.Proc}");

            return @"
" + $"{Name}:" + @"
  Table: " + Table + @"
  Properties:
  " + string.Join(Environment.NewLine + "  ", props) + @"
  Behaviors:
  " + string.Join(Environment.NewLine + "  ", behavs) + @"
";
        }

        public override string ToString()
        {
            return $"{Name ?? "(unknown)"} ({Table ?? "(unknown)"}) Props: {Properties.Count} Behaviors: {Behaviors.Count}";
        }
    }

}
