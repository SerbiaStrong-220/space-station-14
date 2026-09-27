// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.

using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Weapons.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InternalMagazineComponent : Component
{
    [DataField("magSlot")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string MagSlotId = "gun_magazine";

    [DataField]
    public ProtoId<ToolQualityPrototype> RequiredQuality = "Screwing";

    [DataField]
    public TimeSpan TimeToFix = new TimeSpan(0, 0, 10);

    [DataField]
    [AutoNetworkedField]
    public bool MagFixed = true;

    [DataField]
    [AutoNetworkedField]
    public bool MagDetachable = false;
}
