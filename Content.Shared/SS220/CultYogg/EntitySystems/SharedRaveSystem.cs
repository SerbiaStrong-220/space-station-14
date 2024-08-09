// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.StatusEffect;
using Content.Shared.SS220.CultYogg.Components;

namespace Content.Shared.SS220.CultYogg.EntitySystems;

public abstract class SharedRaveSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string EffectkKey = "Rave";
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public void TryApplyRavenness(EntityUid uid, float time, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (!_statusEffectsSystem.HasStatusEffect(uid, EffectkKey, status))
        {
            _statusEffectsSystem.TryAddStatusEffect<RaveComponent>(uid, EffectkKey, TimeSpan.FromSeconds(time), true, status);
        }
        else
        {
            _statusEffectsSystem.TryAddTime(uid, EffectkKey, TimeSpan.FromSeconds(time), status);
        }
    }

    public void TryRemoveRavenness(EntityUid uid)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, EffectkKey);
    }
    public void TryRemoveRavenessTime(EntityUid uid, double timeRemoved)
    {
        _statusEffectsSystem.TryRemoveTime(uid, EffectkKey, TimeSpan.FromSeconds(timeRemoved));
    }

}
