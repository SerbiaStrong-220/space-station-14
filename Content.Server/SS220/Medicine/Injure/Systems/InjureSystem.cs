// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Medicine.Injure;
using Content.Shared.SS220.Medicine.Injure.Components;

namespace Content.Server.SS220.Medicine.Injure.Systems;

public sealed partial class InjureSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InjuredComponent, InjureAddedEvent>(OnInjureAdded);
        SubscribeLocalEvent<InjuredComponent, InjureRemovedEvent>(OnInjureRemoved);
    }

    public void OnInjureAdded(EntityUid uid, InjuredComponent component, InjureAddedEvent ev)
    {
    }
    public void OnInjureRemoved(EntityUid uid, InjuredComponent component, InjureRemovedEvent ev)
    {
    }

}