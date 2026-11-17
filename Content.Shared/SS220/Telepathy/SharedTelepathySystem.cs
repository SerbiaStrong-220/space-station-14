// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Telepathy;

/// <summary>
/// Notifies local listeners when telepathy availability or replicated settings change.
/// </summary>
public abstract class SharedTelepathySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelepathyComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TelepathyComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<TelepathyComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnStartup(Entity<TelepathyComponent> ent, ref ComponentStartup args)
    {
        NotifyChanged(ent.Owner);
    }

    private void OnRemove(Entity<TelepathyComponent> ent, ref ComponentRemove args)
    {
        NotifyChanged(ent.Owner);
    }

    private void OnAfterHandleState(Entity<TelepathyComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        NotifyChanged(ent.Owner);
    }

    private void NotifyChanged(EntityUid uid)
    {
        var ev = new TelepathyChangedEvent(uid);
        RaiseLocalEvent(ref ev);
    }
}
