using System;
using System.Collections.Generic;
using System.Text;

namespace Harp.Core.Models
{
    public class Behavior
    {
        public string Name { get; set; }
        public string StoredProcName { get; set; }
        public bool IsMapped => !string.IsNullOrWhiteSpace(StoredProcName);
    }
}
