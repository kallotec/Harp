using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace Harp.Core.Models
{
    public class Behavior
    {
        public string Name { get; set; }
        public string Proc { get; set; }
        [YamlIgnore]
        public bool IsMapped => !string.IsNullOrWhiteSpace(Proc);

        public override string ToString() => $"{Name ?? "(empty)"} ({Proc ?? "(empty)"})";
    }
}
