// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Content.Shared.Damage;

namespace Content.Shared.SS220.CultYogg.Pod;

[RegisterComponent]
public sealed partial class CultYoggPodComponent : Component
{
    /// <summary>
    /// Time between each healing incident
    /// </summary>
    [DataField]
    public TimeSpan HealingFreq = TimeSpan.FromSeconds(1);

    [DataField]
    public DamageSpecifier Heal = new DamageSpecifier // god forgive me for hardcoding values
    {
        DamageDict = new()
        {
            { "Slash", -6 },
            { "Blunt", -6 },
            { "Piercing", -6},
            {"Heat", -4},
            {"Cold", -4},
            {"Shock", -4},
            {"Airloss", -5},
            { "Radiation", -1 },
            { "Stamina", -5 }
        }
    };

    [DataField]
    public float BloodlossModifier = -4;
    public ContainerSlot MobContainer = default!;

    [Serializable, NetSerializable]
    public enum CultPodVisuals : byte
    {
        Inserted,
    }
}
