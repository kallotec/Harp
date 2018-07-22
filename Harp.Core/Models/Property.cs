using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace Harp.Core.Models
{
    public class Property
    {
        public string Name { get; set; }
        public string Column { get; set; }
        [YamlIgnore]
        public bool IsMapped => !string.IsNullOrWhiteSpace(Column);
    }
}
