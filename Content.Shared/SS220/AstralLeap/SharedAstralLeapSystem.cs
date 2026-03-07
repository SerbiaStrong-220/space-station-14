// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;

namespace Content.Shared.SS220.AstralLeap;

public sealed class SharedAstralLeapSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AstralLeapComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<AstralLeapComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<AstralLeapComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.AstralActionEntity, ent.Comp.AstralAction);
    }

    private void OnShutdown(Entity<AstralLeapComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.AstralActionEntity);
    }
}
