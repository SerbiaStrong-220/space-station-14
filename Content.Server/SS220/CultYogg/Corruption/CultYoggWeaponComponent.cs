// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CultYogg.Corruption;

[RegisterComponent]

public sealed partial class CultYoggWeaponComponent : Component
{
    /// <summary>
    /// how much time required to cocoon enity
    /// </summary>
    [DataField]
    public TimeSpan CocooningCooldown = TimeSpan.FromSeconds(5);

    public TimeSpan? BeforeCocooningTime;
    /// <summary>
    /// What kind of entity it will cocoon in.
    /// </summary>
    [DataField("item", required: true)]
    public ProtoId<EntityPrototype>? Item { get; private set; }
}
