// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using System.Text.Json.Serialization;
using Content.Shared.Damage;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.MiGo;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultYoggHealComponent : Component
{
    /// <summary>
    /// Damage that heals in a single incident
    /// </summary>
    [DataField(required: true)]
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
    public float BloodlossModifier = -1f;
    /// <summary>
    /// Time between each healing incident
    /// </summary>
    public TimeSpan TimeBetweenIncidents = TimeSpan.FromSeconds(2.5); // most balanced value

    public TimeSpan? NextIncidentTime;

    [DataField("sprite")]
    public SpriteSpecifier.Rsi Sprite = new(new("SS220/Effects/cult_yogg_healing.rsi"), "healingEffect");
}
