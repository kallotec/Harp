using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text;

namespace Harp.Core.Models
{
    public class HarpFile
    {
        public List<Entity> Entities { get; set; } = new List<Entity>();
        public bool IsFullyMapped => Entities.All(e => e.IsFullyMapped);

    }

}
