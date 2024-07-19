// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.WristWatch.UI;
using Content.Shared.SS220.WristWatch;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.WristWatch;

public sealed class WristWatchSystem : SharedWristWatchSystem<WristWatchComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<WristWatchStylePrototype>())
            return;

        var query = AllEntityQuery<WristWatchComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            var ui = GetUI(uid);
            ui?.RefreshStyle();
        }
    }

    private WristWatchBoundUserInterface? GetUI(Entity<UserInterfaceComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return null;
        return entity.Comp.ClientOpenInterfaces.GetValueOrDefault(WristWatchUiKey.Key) as WristWatchBoundUserInterface;
    }
}
