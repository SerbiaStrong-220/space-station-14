// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using System.Text.Json.Serialization;
using Content.Shared.Damage;

namespace Content.Shared.SS220.CultYogg.Components;

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
        }
    };
    /// <summary>
    /// Time between each healing incident
    /// </summary>
    public float TimeBetweenIncidents = 2.5f; // most balanced value

    public float NextIncidentTime;//ToDo make it timespan
}
