// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.CultYogg.CorruptInteractions;

[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class DamageCorruptInteractionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField]
    public SoundSpecifier? DamageSound;
}
