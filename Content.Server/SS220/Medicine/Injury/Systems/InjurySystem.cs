// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Medicine.Injury;
using Content.Shared.SS220.Medicine.Injury.Components;

namespace Content.Server.SS220.Medicine.Injury.Systems;

public sealed partial class InjureSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InjuriesContainerComponent, InjuryAddedEvent>(OnInjureAdded);
        SubscribeLocalEvent<InjuriesContainerComponent, InjuryRemovedEvent>(OnInjureRemoved);
    }

    public void OnInjureAdded(EntityUid uid, InjuriesContainerComponent component, InjuryAddedEvent ev)
    {
    }
    public void OnInjureRemoved(EntityUid uid, InjuriesContainerComponent component, InjuryRemovedEvent ev)
    {
    }

}