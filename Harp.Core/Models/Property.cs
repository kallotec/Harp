using System;
using System.Collections.Generic;
using System.Text;

namespace Harp.Core.Models
{
    public class Property
    {
        public string Name { get; set; }
        public string ColumnName { get; set; }
        public bool IsMapped => !string.IsNullOrWhiteSpace(ColumnName);
    }
}
