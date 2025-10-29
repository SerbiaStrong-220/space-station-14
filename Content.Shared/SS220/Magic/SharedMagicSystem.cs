using Content.Shared.Damage;
using Content.Shared.Magic.Events;
using Content.Shared.Popups;
using Content.Shared.Mobs.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.SS220.Magic;

/// <summary>
/// Handles learning and using spells (actions)
/// </summary>

public sealed class SharedMagicSystemSS220 : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TouchHealSpellEvent>(OnTouchHealSpell);
    }

    #region Spells
    #region Touch Spells
    /// <summary>
    /// Healing touch — heals the target and outputs a pop-up.
    /// </summary>
    private void OnTouchHealSpell(TouchHealSpellEvent ev)
    {
        if (ev.Handled || ev.Target == EntityUid.Invalid)
            return;

        if (!HasComp<MobStateComponent>(ev.Target))
        {
            _popup.PopupClient(Loc.GetString("cant-heal-this"), ev.Target, ev.Target);
            return;
        }

        ev.Handled = true;

        _damageable.TryChangeDamage(ev.Target, ev.Heal, true);

        _popup.PopupClient(Loc.GetString("you-feel-healed"), ev.Target, ev.Target);
    }
    // End Touch Spells
    #endregion
    // End Spells
    #endregion
}
