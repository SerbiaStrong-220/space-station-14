// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Emag.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;

namespace Content.Shared.SS220.CultYogg.EntitySystems;

public abstract class SharedMiGoErectSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
    }
}
