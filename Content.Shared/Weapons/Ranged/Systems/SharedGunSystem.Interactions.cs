using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Utility;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.DoAfter;
using Robust.Shared.Map;
using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.Timing;
using Content.Shared.Item;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Projectiles;
using System.Linq;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    private void OnExamine(EntityUid uid, GunComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !component.ShowExamineText)
            return;

        using (args.PushGroup(nameof(GunComponent)))
        {
            args.PushMarkup(Loc.GetString("gun-selected-mode-examine", ("color", ModeExamineColor),
                ("mode", GetLocSelector(component.SelectedMode))));
            args.PushMarkup(Loc.GetString("gun-fire-rate-examine", ("color", FireRateExamineColor),
                ("fireRate", $"{component.FireRateModified:0.0}")));
        }
    }

    private string GetLocSelector(SelectiveFire mode)
    {
        return Loc.GetString($"gun-{mode.ToString()}");
    }

    private void OnAltVerb(EntityUid uid, GunComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.SelectedMode == component.AvailableModes)
            return;

        var nextMode = GetNextMode(component);

        AlternativeVerb verb = new()
        {
            Act = () => SelectFire(uid, component, nextMode, args.User),
            Text = Loc.GetString("gun-selector-verb", ("mode", GetLocSelector(nextMode))),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),
        };

        args.Verbs.Add(verb);
    }

    private SelectiveFire GetNextMode(GunComponent component)
    {
        var modes = new List<SelectiveFire>();

        foreach (var mode in Enum.GetValues<SelectiveFire>())
        {
            if ((mode & component.AvailableModes) == 0x0)
                continue;

            modes.Add(mode);
        }

        var index = modes.IndexOf(component.SelectedMode);
        return modes[(index + 1) % modes.Count];
    }

    private void SelectFire(EntityUid uid, GunComponent component, SelectiveFire fire, EntityUid? user = null)
    {
        if (component.SelectedMode == fire)
            return;

        DebugTools.Assert((component.AvailableModes  & fire) != 0x0);
        component.SelectedMode = fire;

        if (!Paused(uid))
        {
            var curTime = Timing.CurTime;
            var cooldown = TimeSpan.FromSeconds(InteractNextFire);

            if (component.NextFire < curTime)
                component.NextFire = curTime + cooldown;
            else
                component.NextFire += cooldown;
        }

        Audio.PlayPredicted(component.SoundMode, uid, user);
        Popup(Loc.GetString("gun-selected-mode", ("mode", GetLocSelector(fire))), uid, user);
        Dirty(uid, component);
    }

    /// <summary>
    /// Cycles the gun's <see cref="SelectiveFire"/> to the next available one.
    /// </summary>
    public void CycleFire(EntityUid uid, GunComponent component, EntityUid? user = null)
    {
        // Noop
        if (component.SelectedMode == component.AvailableModes)
            return;

        DebugTools.Assert((component.AvailableModes & component.SelectedMode) == component.SelectedMode);
        var nextMode = GetNextMode(component);
        SelectFire(uid, component, nextMode, user);
    }

    // TODO: Actions need doing for guns anyway.
    private sealed partial class CycleModeEvent : InstantActionEvent
    {
        public SelectiveFire Mode = default;
    }

    private void OnCycleMode(EntityUid uid, GunComponent component, CycleModeEvent args)
    {
        SelectFire(uid, component, args.Mode, args.Performer);
    }

    private void OnGunSelected(EntityUid uid, GunComponent component, HandSelectedEvent args)
    {
        if (Timing.ApplyingState)
            return;

        if (component.FireRateModified <= 0)
            return;

        var fireDelay = 1f / component.FireRateModified;
        if (fireDelay.Equals(0f))
            return;

        if (!component.ResetOnHandSelected)
            return;

        if (Paused(uid))
            return;

        // If someone swaps to this weapon then reset its cd.
        var curTime = Timing.CurTime;
        var minimum = curTime + TimeSpan.FromSeconds(fireDelay);

        if (minimum < component.NextFire)
            return;

        component.NextFire = minimum;
        Dirty(uid, component);
    }

    ///SS220-new-feature kus start
    private void OnGetVerbs(Entity<GunComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        // Добавляю возможность убиться ко всему маленькому стрелковому оружию
        if (TryComp<ItemComponent>(entity, out var item))
        {
            if (!entity.Comp.CanSuicide && _item.GetSizePrototype(item.Size).Weight <= 3 && !entity.Comp.ClumsyProof)
            {
                entity.Comp.CanSuicide = true;
            }
        }

        if (!args.CanAccess || !args.CanInteract || !entity.Comp.CanSuicide)
            return;

        var user = args.User;
        if (!_hands.IsHolding(user, entity, out _))
            return;

        if (!TryComp<GunComponent>(entity, out var guncomp) || !CanShoot(guncomp))
            return;

        Verb verb = new()
        {
            Act = () =>
            {
                var doAfter = new DoAfterArgs(EntityManager, user, 5f, new SuicideDoAfterEvent(), entity, target: user, used: entity)
                {
                    BreakOnMove = true,
                    BreakOnHandChange = true,
                    BreakOnDamage = true,
                    NeedHand = true,
                    Broadcast = true
                };

                if (_doAfter.TryStartDoAfter(doAfter))
                {
                    PopupSystem.PopupPredicted(Loc.GetString("suicide-start-popup-self",
                            ("weapon", MetaData(entity).EntityName)), user, user);

                    PopupSystem.PopupEntity(Loc.GetString("suicide-start-popup-others",
                            ("user", MetaData(user).EntityName),
                            ("weapon", MetaData(entity).EntityName)), user, Filter.PvsExcept(user), true);
                }
            },
            Text = Loc.GetString("suicide-verb-name"),
            Priority = 1
        };

        args.Verbs.Add(verb);
    }

    private void OnDoSuicideComplete(SuicideDoAfterEvent args)
    {

        if (!_netManager.IsServer)
            return;

        if (args.Cancelled || args.Handled || args.Used == null)
        {
            var looser = args.User;
            PopupSystem.PopupPredicted(Loc.GetString("suicide-failed-popup"), looser, looser);
            return;
        }

        var user = args.User;
        var weapon = args.Used.Value;

        if (!_hands.IsHolding(user, weapon, out _))
        {
            return;
        }

        if (!TryComp<GunComponent>(weapon, out var guncomp))
        {
            return;
        }

        var coordsFrom = Transform(weapon).Coordinates;
        var coordsTo = new EntityCoordinates(user, new Vector2(coordsFrom.X + 1f, coordsFrom.Y));
        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), coordsFrom, null);
        RaiseLocalEvent(weapon, ev);
        var damage = new DamageSpecifier();
        var damageType = "";

        if (ev.Ammo.Count == 0)
            return;

        /// В текущей реализации можно застрелиться из условново МК с любыми патронами.
        /// Смерть не случиться если их урон < 4, это травматические, учебные...
        /// Для нанесения смертельного урона используеться наибольший тип урона в патроне.

        var shootableAmmo = ev.Ammo[0].Shootable;

        if (ev.Ammo[0].Shootable is HitscanPrototype hitscan) // лазеры
        {
            if (hitscan.Damage?.DamageDict != null && hitscan.Damage.DamageDict.Count > 0)
            {
                damageType = hitscan.Damage?.DamageDict?.Where(kv => kv.Value > 3).OrderByDescending(kv => kv.Value).FirstOrDefault().Key ?? "";
            }
        }
        else if (ev.Ammo[0].Entity is { } ent && TryComp<ProjectileComponent>(ent, out var projectile)) // револьверы
        {
            if (projectile.Damage?.DamageDict != null && projectile.Damage.DamageDict.Count > 0)
            {
                damageType = projectile.Damage?.DamageDict?.Where(kv => kv.Value > 3).OrderByDescending(kv => kv.Value).FirstOrDefault().Key ?? "";
            }
        }
        else if (ev.Ammo[0].Entity is { } ent2 && TryComp<CartridgeAmmoComponent>(ent2, out var cartridgeComp)) // Патрон в патроннике
        {
            if (!ProtoManager.TryIndex<EntityPrototype>((cartridgeComp).Prototype.Id, out var protoNamePrototipe))
            {
                return;
            }
            if (!ProtoManager.TryIndex<EntityPrototype>(protoNamePrototipe, out var protoPrototype))
            {
                return;
            }
            if (protoPrototype.Components.TryGetValue("Projectile", out var projectileReg)
                && projectileReg.Component is ProjectileComponent projectileComponent)
            {
                if (projectileComponent.Damage?.DamageDict != null
                    && projectileComponent.Damage.DamageDict.Count > 0)
                {
                    damageType = projectileComponent.Damage?.DamageDict?.Where(kv => kv.Value > 3).OrderByDescending(kv => kv.Value).FirstOrDefault().Key ?? "";
                }
            }
        }

        damage.DamageDict.Add(damageType, 200);
        Shoot(weapon, guncomp, ev.Ammo, coordsFrom, coordsTo, out _);
        // Мб ещё админлоги добавить
        Timer.Spawn(200, () =>
        {
            Damageable.TryChangeDamage(user, damage, true);
        });

        args.Handled = true;
    }
    ///SS220-new-feature kus end
}
