using Content.Shared.Random;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.VariationPass.Components;

/// <summary>
/// This is used for spawning entities randomly dotted around the station in a variation pass.
/// </summary>
[RegisterComponent]
public sealed partial class EntitySpawnVariationPassComponent : Component
{
    /// <summary>
    ///     Number of tiles before we spawn one entity on average.
    /// </summary>
    [DataField]
    public float TilesPerEntityAverage = 50f;

    [DataField]
    public float TilesPerEntityStdDev = 7f;

    /// <summary>
    ///     Spawn entries for each chosen location.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Entities = default!;

    // SS220 Fix SM begin
    /// <summary>
    ///     The selected components block the spawn of garbage next to or in the vicinity of the specified entity with chosen component
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist = new();
    // SS220 Fix SM end
}
