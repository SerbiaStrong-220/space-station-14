using Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities.Stealth;
using Content.Server.SS220.DarkForces.Saint.Saintable.Events;
using Content.Shared.SS220.DarkForces.Narsi.Abilities.Events;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.Stealth.Components;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities;

public sealed partial class NarsiCultistAbilitiesSystem
{
    private void InitializeStealth()
    {
        SubscribeLocalEvent<NarsiCultistComponent, NarsiCultistInvisibilityEvent>(OnInvisibilityEvent);
        SubscribeLocalEvent<NarsiCultistStealthComponent, OnSaintEntityCollide>(OnSaintContactInStealth);
        SubscribeLocalEvent<NarsiCultistStealthComponent, OnSaintEntityAfterInteract>(OnSaintContactInStealth);
    }

    private void OnSaintContactInStealth(EntityUid uid, NarsiCultistStealthComponent component, ISaintEntityEvent args)
    {
        CleanupStealth(uid);
    }

    private void OnInvisibilityEvent(EntityUid uid, NarsiCultistComponent component, NarsiCultistInvisibilityEvent args)
    {
        if (args.Handled)
            return;

        var level = _progressSystem.GetAbilityLevel(StealthAction);
        var passiveVisibilityRate = level switch
        {
            1 => -0.15f,
            2 => -0.30f,
            _ => -0.45f
        };
        var movementVisibilityRate = level switch
        {
            1 => 0.2f,
            2 => 0.1f,
            _ => 0.05f
        };
        var duration = level switch
        {
            1 => 30,
            2 => 60,
            _ => 120
        };

        EnsureComp<StealthComponent>(uid);
        var stealthOnMoveComponent = EnsureComp<StealthOnMoveComponent>(uid);
        stealthOnMoveComponent.PassiveVisibilityRate = passiveVisibilityRate;
        stealthOnMoveComponent.MovementVisibilityRate = movementVisibilityRate;

        var cultistStealthComponent = EnsureComp<NarsiCultistStealthComponent>(uid);
        cultistStealthComponent.TickToRemove = _timing.CurTime + TimeSpan.FromSeconds(duration);

        _popupSystem.PopupEntity("Вы стали невидимым", uid, uid);
        OnCultistAbility(uid, args);

        args.Handled = true;
    }

    private void UpdateStealth()
    {
        var query = EntityQueryEnumerator<NarsiCultistStealthComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.TickToRemove > _timing.CurTime)
                continue;

            CleanupStealth(uid);
        }
    }

    private void CleanupStealth(EntityUid uid)
    {
        RemComp<NarsiCultistStealthComponent>(uid);
        RemComp<StealthOnMoveComponent>(uid);
        RemComp<StealthComponent>(uid);
        _popupSystem.PopupEntity("Невидимость закончилась", uid, uid);
    }
}
