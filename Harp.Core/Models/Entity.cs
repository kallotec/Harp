using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace Harp.Core.Models
{
    public class Entity
    {
        [YamlIgnore]
        public string Name { get; set; }
        public string Table { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Behaviors { get; set; } = new Dictionary<string, string>();
        
        [YamlIgnore]
        public bool IsFullyMapped => (Properties.Any() && Properties.All(p => !string.IsNullOrWhiteSpace(p.Value)) &&
                                      Behaviors.Any() && Behaviors.All(b => !string.IsNullOrWhiteSpace(b.Value)));

        public void AddProperty(string name, string column) => Properties.Add(name, column);
        public void AddBehavior(string name, string proc) => Behaviors.Add(name, proc);


        public string GenerateHarpFileFragment()
        {
            throw new NotImplementedException();
//            var props = Properties.Select(p => $"- {p.Name}: {p.Column}");
//            var behavs = Behaviors.Select(b => $"- {b.Name}: {b.Proc}");

//            return @"
//" + $"{Name}:" + @"
//  Table: " + Table + @"
//  Properties:
//  " + string.Join(Environment.NewLine + "  ", props) + @"
//  Behaviors:
//  " + string.Join(Environment.NewLine + "  ", behavs) + @"
//";
        }

        public override string ToString()
        {
            return $"{Name ?? "(unknown)"} ({Table ?? "(unknown)"}) Props: {Properties.Count} Behaviors: {Behaviors.Count}";
        }
    }

}
