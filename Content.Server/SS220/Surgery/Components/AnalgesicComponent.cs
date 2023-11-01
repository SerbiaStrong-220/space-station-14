using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.SS220.Surgery.Components
{
    [RegisterComponent]
    public sealed partial class AnalgesicComponent : Component
    {
        /// <summary>
        /// aboba 
        /// </summary>
        [DataField("effectiveness")]
        public float Effectiveness { get; set; }
    }
}
