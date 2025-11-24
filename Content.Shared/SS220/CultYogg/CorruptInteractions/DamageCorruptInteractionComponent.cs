// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.CultYogg.CorruptInteractions;

[RegisterComponent]
public sealed partial class DamageCorruptInteractionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = new();
}
