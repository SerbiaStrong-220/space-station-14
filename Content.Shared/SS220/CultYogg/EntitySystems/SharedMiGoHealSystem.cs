// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.StatusEffect;
using Content.Shared.SS220.CultYogg.Components;

namespace Content.Shared.SS220.CultYogg.EntitySystems;

public abstract class SharedMiGoHealSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string EffectkKey = "MiGoHeal";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MiGoComponent, MiGoSetHealStatusEvent>(SetupMiGoHeal);//IDK if its good to recieve it from another component
    }
    private void SetupMiGoHeal(Entity<MiGoComponent> uid, ref MiGoSetHealStatusEvent args)
    {
        TryApplyMiGoHeal(uid, args.HealTime);
    }
    public void TryApplyMiGoHeal(EntityUid uid, float time, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (!_statusEffectsSystem.HasStatusEffect(uid, EffectkKey, status))
        {
            _statusEffectsSystem.TryAddStatusEffect<MiGoHealComponent>(uid, EffectkKey, TimeSpan.FromSeconds(time), true, status);
        }
        else
        {
            _statusEffectsSystem.TryAddTime(uid, EffectkKey, TimeSpan.FromSeconds(time), status);
        }
    }

    public void TryRemoveMiGoHeal(EntityUid uid)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, EffectkKey);
    }
    public void TryRemoveMiGoHealTime(EntityUid uid, double timeRemoved)
    {
        _statusEffectsSystem.TryRemoveTime(uid, EffectkKey, TimeSpan.FromSeconds(timeRemoved));
    }
}

/// <summary>
/// Event that is raised whenever someone is implanted with any given implant.
/// Raised on the the implant entity.
/// </summary>
/// <remarks>
/// implant implant implant implant
/// </remarks>
[ByRefEvent]
public readonly struct MiGoSetHealStatusEvent
{
    public readonly EntityUid Target;
    public readonly float HealTime;

    public MiGoSetHealStatusEvent(EntityUid target, float healTime)
    {
        Target = target;
        HealTime = healTime;
    }
}
