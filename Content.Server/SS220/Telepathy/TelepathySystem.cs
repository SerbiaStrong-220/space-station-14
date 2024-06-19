using Content.Server.Actions;

namespace Content.Server.SS220.Telepathy;

/// <summary>
/// This handles telepathy
/// </summary>
public sealed class TelepathySystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TelepathyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, TelepathyComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
    }
}
