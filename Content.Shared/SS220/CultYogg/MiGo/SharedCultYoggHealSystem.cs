// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.StatusEffect;

namespace Content.Shared.SS220.CultYogg.MiGo;

public abstract class SharedCultYoggHealSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string EffectkKey = "MiGoHeal";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
    public void TryApplyMiGoHeal(EntityUid uid, float time, StatusEffectsComponent? statusComp = null)
    {
        if (!Resolve(uid, ref statusComp, false))
            return;

        if (!_statusEffectsSystem.HasStatusEffect(uid, EffectkKey, statusComp))
        {
            _statusEffectsSystem.TryAddStatusEffect<CultYoggHealComponent>(uid, EffectkKey, TimeSpan.FromSeconds(time), true, statusComp);
        }
        else
        {
            _statusEffectsSystem.TryAddTime(uid, EffectkKey, TimeSpan.FromSeconds(time), statusComp);
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
