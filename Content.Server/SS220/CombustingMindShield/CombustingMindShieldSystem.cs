// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CombustingMindShield;

public sealed class CombustingMindShieldSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombustingMindShieldComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<CombustingMindShieldComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<ImplantedComponent>(ent, out var implanted))
        {
            var implants = implanted.ImplantContainer.ContainedEntities;

            foreach (var implant in implants)
            {
                if (!HasComp<MindShieldImplantComponent>(implant))
                    continue;

                ent.Comp.Implant = implant;
                break;
            }
        }

        ent.Comp.CombustionTime = _time.CurTime + ent.Comp.BeforeCombustionTime;
        _popup.PopupClient(Loc.GetString("combisting-mindshield-startup"), ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        List<EntityUid> onRemove = [];

        var query = EntityQueryEnumerator<CombustingMindShieldComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (comp.CombustionTime > _time.CurTime)
                continue;

            QueueDel(comp.Implant);
            onRemove.Add(ent);
        }

        foreach (var ent in onRemove)
        {
            RemComp<CombustingMindShieldComponent>(ent);
            _popup.PopupClient(Loc.GetString("combisting-mindshield-deleted"), ent);
        }
    }
}
