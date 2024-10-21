// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Zombies;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Physics.Systems;
using Content.Shared.Tag;
using Robust.Shared.Serialization;
using Content.Shared.StatusEffect;
using Robust.Shared.Timing;
using Content.Shared.NPC.Systems;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.Buckle.Components;
using System.Linq;
using Robust.Shared.Audio.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Alert;

namespace Content.Shared.SS220.CultYogg.EntitySystems;

public abstract class SharedMiGoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedMiGoErectSystem _miGoErectSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedCultYoggHealSystem _heal = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;


    //[Dependency] private readonly CultYoggRuleSystem _cultYoggRule = default!; //maybe use this for enslavement

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, ComponentStartup>(OnCompInit);

        // actions
        SubscribeLocalEvent<MiGoComponent, MiGoHealEvent>(MiGoHeal);
        SubscribeLocalEvent<MiGoComponent, MiGoErectEvent>(MiGoErect);
        SubscribeLocalEvent<MiGoComponent, MiGoSacrificeEvent>(MiGoSacrifice);
    }

    protected virtual void OnCompInit(Entity<MiGoComponent> uid, ref ComponentStartup args)
    {
        _actions.AddAction(uid, ref uid.Comp.MiGoHealActionEntity, uid.Comp.MiGoHealAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoEnslavementActionEntity, uid.Comp.MiGoEnslavementAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoAstralActionEntity, uid.Comp.MiGoAstralAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoErectActionEntity, uid.Comp.MiGoErectAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoSacrificeActionEntity, uid.Comp.MiGoSacrificeAction);
    }

    public virtual void ChangeForm(EntityUid uid, MiGoComponent comp, bool isMaterial) { }

    #region Heal
    private void MiGoHeal(Entity<MiGoComponent> uid, ref MiGoHealEvent args)
    {
        if (args.Handled)
            return;

        /*
        if (!HasComp<CultYoggComponent>(args.Target))//ToDo should discuss
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cult-yogg-heal-only-cultists"), uid);
            return;
        }
        */
        /*
        //check if effect is already applyed
        if (_statusEffectsSystem.HasStatusEffect(args.Target, uid.Comp.RequiedEffect))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-should-eat-shroom"), args.Target, uid);
            return;
        }
        */

        _heal.TryApplyMiGoHeal(args.Target, uid.Comp.HealingEffectTime);

        args.Handled = true;
    }
    #endregion

    #region Erect
    private void MiGoErect(Entity<MiGoComponent> entity, ref MiGoErectEvent args)
    {
        //will wait when sw will update ui parts to copy paste, cause rn it has an errors
        if (args.Handled || !TryComp<ActorComponent>(entity, out var actor))
            return;
        //args.Handled = true; // No cooldown for UI

        _miGoErectSystem.OpenUI(entity, actor);
    }
    #endregion

    #region MiGoSacrifice
    private void MiGoSacrifice(Entity<MiGoComponent> uid, ref MiGoSacrificeEvent args)
    {
        var altarQuery = EntityQueryEnumerator<CultYoggAltarComponent, TransformComponent>();

        while (altarQuery.MoveNext(out var altarUid, out var altarComp, out _))
        {
            if (!_transform.InRange(Transform(uid).Coordinates, Transform(altarUid).Coordinates, altarComp.RitualStartRange))
                continue;

            if (!TryComp<StrapComponent>(altarUid, out var strapComp))
                continue;

            if (!strapComp.BuckledEntities.Any())
                continue;

            if (!HasComp<CultYoggSacrificialComponent>(strapComp.BuckledEntities.First()))
                continue;

            TryDoSacrifice(altarUid, uid, altarComp);
        }
    }
    public bool TryDoSacrifice(EntityUid altarUid, EntityUid user, CultYoggAltarComponent altarComp)
    {
        if (altarComp == null)
            return false;

        if (!TryComp<StrapComponent>(altarUid, out var strapComp))
            return false;

        var targetUid = strapComp.BuckledEntities.FirstOrDefault();
        var migoQuery = EntityQueryEnumerator<MiGoComponent>();
        var currentMiGoAmount = 0;

        while (migoQuery.MoveNext(out var migoUid, out var miGoComponent))
        {
            if (miGoComponent == null)
                continue;

            if (_transform.InRange(Transform(migoUid).Coordinates, Transform(altarUid).Coordinates,  altarComp.RitualStartRange))
                currentMiGoAmount++;
        }

        if (currentMiGoAmount < altarComp.RequiredAmountMiGo)
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cult-yogg-altar-not-enough-migo"), user, user);

            return false;
        }

        var sacrificeDoAfter = new DoAfterArgs(EntityManager, user, altarComp.RutualTime, new MiGoSacrificeDoAfterEvent(), altarUid, target: targetUid)
        {
            BreakOnDamage = true,
            BreakOnMove = true
        };

        var started = _doAfter.TryStartDoAfter(sacrificeDoAfter);

        if (started)
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cult-yogg-sacrifice-started", ("user", user), ("target", targetUid)),
                 altarUid, PopupType.MediumCaution);

            //_audio.PlayPredicted(altarComp.RitualSound, user, user); // TODO: ritual sound(mythic)
        }

        return started;
    }

    #endregion

    public override void Update(float delta)
    {
        base.Update(delta);
        var query = EntityQueryEnumerator<MiGoComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (IsPaused(uid))
                continue;

            if (comp.MaterializationTime == null)
                continue;

            var secondsLeft = (FixedPoint2)Math.Round((comp.MaterializationTime.Value - _timing.CurTime).TotalSeconds);//calculate time left in seconds

            if (comp.AlertTime == 0 || comp.AlertTime > secondsLeft)//update alert if buffer has a different value
            {
                comp.AlertTime = secondsLeft;
                _alerts.ShowAlert(uid, comp.AstralAlert);
            }

            if (_timing.CurTime <= comp.MaterializationTime.Value)
                continue;

            ChangeForm(uid, comp, true);
            if (!comp.AudioPlayed)
            {
                _audio.PlayEntity(comp.SoundMaterialize, uid, uid);
                comp.AudioPlayed = true;
            }
            _actions.StartUseDelay(comp.MiGoAstralActionEntity);
        }
    }
}

[Serializable, NetSerializable]
public sealed partial class MiGoSacrificeDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class MiGoEnslaveDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class AfterMaterialize : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class AfterDeMaterialize : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[ByRefEvent, Serializable]
public record struct CultYoggEnslavedEvent(EntityUid? Target);

[NetSerializable, Serializable]
public enum MiGoTimerVisualLayers : byte
{
    Digit1,
    Digit2
}
[Serializable, NetSerializable]
public enum MiGoVisual
{
    Base,
    Astral
}
