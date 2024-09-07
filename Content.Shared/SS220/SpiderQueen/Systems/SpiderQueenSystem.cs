// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.SS220.SpiderQueen.Components;

namespace Content.Shared.SS220.SpiderQueen.Systems;

public sealed partial class SpiderQueenSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderQueenComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<SpiderQueenComponent> ent, ref ComponentStartup args)
    {
        var (uid, component) = ent;
        if (component.Actions != null)
        {
            foreach (var action in component.Actions)
            {
                if (string.IsNullOrWhiteSpace(action))
                    continue;

                _actionsSystem.AddAction(uid, action);
            }
        }
    }
}
