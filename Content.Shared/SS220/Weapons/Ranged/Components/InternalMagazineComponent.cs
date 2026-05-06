// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT

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
    public string MagSlotId = "internal_mag";

    [DataField]
    public ProtoId<ToolQualityPrototype> RequiredQuality = "Screwing";

    [DataField]
    public TimeSpan TimeToFix = new TimeSpan(0, 0, 10);

    [DataField]
    [AutoNetworkedField]
    public bool magFixed = true;
}
