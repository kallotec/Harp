using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Harp.Core.Models
{
    public class Entity
    {
        public string EntityName { get; set; }
        public string TableName { get; set; }
        public List<Property> Properties { get; set; } = new List<Property>();
        public List<Behavior> Behaviors { get; set; } = new List<Behavior>();
        public bool IsFullyMapped => (Properties.All(p => p.IsMapped) && Behaviors.All(b => b.IsMapped));

        public string GenerateHarpFileFragment()
        {
            var props = Properties.Select(p => $"- {p.Name}: {p.ColumnName}");
            var behavs = Behaviors.Select(b => $"- {b.Name}: {b.StoredProcName}");

            return @"
" + $"{EntityName}:" + @"
  Table: " + TableName + @"
  Properties:
  " + string.Join(Environment.NewLine + "  ", props) + @"
  Behaviors:
  " + string.Join(Environment.NewLine + "  ", behavs) + @"
";
        }

    }

}
