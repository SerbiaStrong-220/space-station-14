// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.CultMiniMap;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CultYogg.CultMiniMap;

/// <summary>
/// Refreshes the map after its private component state arrives for the owner.
/// </summary>
public sealed class CultMiniMapClientSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultMiniMapComponent, AfterAutoHandleStateEvent>(OnStateUpdate);
    }

    private void OnStateUpdate(Entity<CultMiniMapComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi(ent.Owner, CultMiniMapUIKey.Key, out var bui))
            bui.Update();
    }
}
