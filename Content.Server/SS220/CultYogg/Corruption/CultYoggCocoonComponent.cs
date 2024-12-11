// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CultYogg.Corruption;

[RegisterComponent]

public sealed partial class CultYoggCocoonComponent : Component
{
    /// <summary>
    ///     The list of entities to spawn, with amounts and orGroups.
    /// </summary>
    [DataField("item", required: true)]
    public ProtoId<EntityPrototype>? Item { get; private set; }
    //public List<EntitySpawnEntry> Items = new();

    /// <summary>
    ///     A sound to play when the items are spawned. For example, gift boxes being unwrapped.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound = null;
}
