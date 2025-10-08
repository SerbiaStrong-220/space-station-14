// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Chemistry.MobThresholdsModifierStatusEffect;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MobThresholdsModifierStatusEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<MobState, ModifierData> Modifiers = [];

    [Serializable, NetSerializable, DataDefinition]
    public partial struct ModifierData()
    {
        [DataField]
        public FixedPoint2 Multiplier = 1.0f;

        [DataField]
        public FixedPoint2 Flat = 0f;
    }
}
