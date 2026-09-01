// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Chemistry.MobThresholdsModifierStatusEffect;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MobThresholdsModifierStatusEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<MobState, MobThresholdsModifier> Modifiers = [];
}
