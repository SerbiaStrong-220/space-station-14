// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.ClinkGlasses;

namespace Content.Server.SS220.ClinkGlasses;

[RegisterComponent]
[Access(typeof(ClinkGlassesSystem))]
public sealed partial class ClinkGlassesReceiverComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Initiator { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Item { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public float ReceiveRange = 2f;
}
