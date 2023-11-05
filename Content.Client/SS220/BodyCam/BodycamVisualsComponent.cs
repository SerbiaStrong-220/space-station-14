// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Bodycam;

namespace Content.Client.SS220.Bodycam;

// Dummy component so that targetted events work on client for
// appearance events.
[RegisterComponent]
public sealed partial class BodycamVisualsComponent : Component
{
    [DataField("sprites")]
    public Dictionary<BodycamVisuals, string> CameraSprites = new();
}
