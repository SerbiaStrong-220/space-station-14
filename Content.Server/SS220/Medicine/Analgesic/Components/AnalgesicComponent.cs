// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.SS220.Medicine.Surgery.Components;

[RegisterComponent]
public sealed partial class AnalgesicComponent : Component
{
    /// <summary>
    /// aboba 
    /// </summary>
    [DataField("effectiveness")]
    public float Effectiveness { get; set; }
}
