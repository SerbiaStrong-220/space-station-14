// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.SS220.SpiderQueen.Components;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.SpiderQueen.Systems;

public abstract class SharedSpiderQueenSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderQueenComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SpiderQueenComponent, ExaminedEvent>(OnExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpiderQueenComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextSecond)
                continue;

            comp.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);

            var newValue = comp.CurrentMana + comp.PassiveGeneration;
            comp.CurrentMana = newValue > comp.MaxMana
                ? comp.MaxMana
                : newValue;
            Dirty(uid, comp);
        }
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

    private void OnExamine(Entity<SpiderQueenComponent> entity, ref ExaminedEvent args)
    {
        if (args.Examined == args.Examiner && entity.Comp.ShouldShowMana)
        {
            args.PushMarkup(Loc.GetString("spider-queen-mana-amount",
                ("current", entity.Comp.CurrentMana.Int()), ("max", entity.Comp.MaxMana.Int())));
        }
    }

    /// <summary>
    /// Checks if there is enough mana for some action
    /// </summary>
    public bool CheckEnoughMana(EntityUid uid, SpiderQueenComponent component, FixedPoint2 cost)
    {
        return component.CurrentMana >= cost;
    }
}
