// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Barricade;

/// <summary>
/// A component that allows entity to block projectiles or hitscans with a specific chance that change by fire distance
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBarricadeSystem))]
public sealed partial class BarricadeComponent : Component
{
    /// <summary>
    /// Chance to catch the projectile, if the distance between barricade and the shooter <= <see cref="MinDistance"/>
    /// </summary>
    [DataField]
    public float MinHitChance = 0;
    /// <summary>
    /// Chance to catch the projectile, if the distance between barricade and the shooter >= <see cref="MaxDistance"/>
    /// </summary>
    [DataField]
    public float MaxHitChance = 0.7f;

    [DataField]
    public float MinDistance = 1.5f;
    [DataField]
    public float MaxDistance = 15f;

    /// <summary>
    /// A whitelist of entities that will always pass through the barricade
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
}
