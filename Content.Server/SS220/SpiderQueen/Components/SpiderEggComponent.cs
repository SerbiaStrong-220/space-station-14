// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Storage;

namespace Content.Server.SS220.SpiderQueen.Components;

[RegisterComponent]
public sealed partial class SpiderEggComponent : Component
{
    /// <summary>
    /// The entity that spawned egg
    /// </summary>
    [DataField]
    public EntityUid? EggOwner;

    /// <summary>
    /// Egg incubation time
    /// </summary>
    [DataField(required: true)]
    public float IncubationTime;

    /// <summary>
    /// A list of prototypes that will be spawn after the incubation time
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> SpawnProtos;
}
