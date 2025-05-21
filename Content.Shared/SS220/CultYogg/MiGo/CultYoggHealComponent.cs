// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.MiGo;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CultYoggHealComponent : Component
{
    /// <summary>
    /// Damage that heals in a single incident
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Heal = new();

    [DataField, AutoNetworkedField]
    public float BloodlossModifier;

    /// <summary>
    /// Restore missing blood.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ModifyBloodLevel;

    [DataField, AutoNetworkedField]
    public float ModifyStamina;

    /// <summary>
    /// Time between each healing incident
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenIncidents = TimeSpan.FromSeconds(2.5); // most balanced value

    public TimeSpan? NextIncidentTime;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi Sprite = new(new("SS220/Effects/cult_yogg_healing.rsi"), "healingEffect");
}
