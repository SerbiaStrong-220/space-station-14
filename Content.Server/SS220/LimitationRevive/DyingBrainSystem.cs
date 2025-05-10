// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.Timing;

namespace Content.Server.SS220.LimitationRevive;

/// <summary>
/// Controls damage dealt on death
/// </summary>
public sealed class DyingBrainSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DyingBrainComponent, ComponentInit>(OnCompInit);
    }

    private void OnCompInit(Entity<DyingBrainComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextIncidentTime = _timing.CurTime + ent.Comp.NextIncidentTime;
    }


    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DyingBrainComponent>();

        while (query.MoveNext(out var uid, out var brainСomp))
        {
            if (brainСomp.NextIncidentTime is null)
                return;

            if (brainСomp.Damage is null)//ToDo SS220 not sure if it might be nullable
                return;

            if (_timing.CurTime < brainСomp.NextIncidentTime)
                return;

            _damageableSystem.TryChangeDamage(uid, brainСomp.Damage, true);

            brainСomp.NextIncidentTime = _timing.CurTime + brainСomp.TimeBetweenIncidents;
        }
    }
}
