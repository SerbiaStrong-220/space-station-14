// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Components;
using Robust.Shared.GameStates;

namespace Content.Client.SS220.CultYogg;

[RegisterComponent, NetworkedComponent]
public sealed partial class RaveComponent : SharedRaveComponent
{
    public EntityUid? EffectEntity = null;
}
