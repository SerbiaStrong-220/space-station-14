// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Explosion.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.SS220.DnaLock;
using Content.Shared.SS220.DnaLock.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DnaLock;

public sealed class DnaLockClothingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly DnaLockSystem _dnaLock = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DnaLockClothingComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<DnaLockClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<DnaLockClothingActiveComponent, DnaLockClothingComponent>();

        while (query.MoveNext(out var uid, out var active, out var clothing))
        {
            if (TerminatingOrDeleted(active.WearerUid))
            {
                RemComp<DnaLockClothingActiveComponent>(uid);
                continue;
            }

            if (curTime >= active.ExplodeAt)
            {
                TriggerExplosion(uid, active, clothing);
                continue;
            }

            // Beeper and popup countdown
            if (curTime >= active.NextBeepAt)
            {
                var timeLeft = (int)(active.ExplodeAt - curTime).TotalSeconds;
                PlayTimerWarning(uid, active.WearerUid, clothing, timeLeft);

                if (clothing.BeepInterval <= TimeSpan.Zero)
                {
                    active.NextBeepAt = curTime + TimeSpan.FromSeconds(1);
                    continue;
                }

                active.NextBeepAt += clothing.BeepInterval;
                while (active.NextBeepAt <= curTime)
                {
                    active.NextBeepAt += clothing.BeepInterval;
                }
            }
        }
    }

    private void OnEquipped(Entity<DnaLockClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!TryComp<DnaLockableComponent>(ent, out var dnaLock))
            return;

        if (_dnaLock.CheckAccess((ent.Owner, dnaLock), args.Wearer, silentFail: true))
            return;

        PlayInitialWarning(ent.Owner, args.Wearer, ent.Comp);

        // Starting the timer
        var curTime = _timing.CurTime;
        var active = EnsureComp<DnaLockClothingActiveComponent>(ent);
        active.WearerUid = args.Wearer;
        active.ExplodeAt = curTime + ent.Comp.TimeToExplode;
        active.NextBeepAt = curTime + ent.Comp.BeepInterval;
    }

    private void OnUnequipped(Entity<DnaLockClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemComp<DnaLockClothingActiveComponent>(ent);
    }

    private void PlayInitialWarning(EntityUid itemUid, EntityUid wearer, DnaLockClothingComponent clothing)
    {
        var wearerName = Identity.Name(wearer, EntityManager, wearer);

        var othersMsg = Loc.GetString(
            clothing.WarningPopupOthers,
            ("wearer", wearerName));

        var selfMsg = Loc.GetString(clothing.WarningPopupWearer);

        _popup.PopupEntity(
            othersMsg,
            wearer,
            Filter.PvsExcept(wearer),
            recordReplay: true,
            type: PopupType.LargeCaution);

        _popup.PopupEntity(selfMsg, wearer, wearer, PopupType.LargeCaution);
        _audio.PlayPvs(clothing.WarningSound, itemUid);
    }

    private void PlayTimerWarning(EntityUid itemUid, EntityUid wearer, DnaLockClothingComponent clothing, int secondsLeft)
    {
        var wearerName = Identity.Name(wearer, EntityManager, wearer);

        var othersMsg = Loc.GetString(
            clothing.TimerPopupOthers,
            ("wearer", wearerName),
            ("seconds", secondsLeft));

        var selfMsg = Loc.GetString(
            clothing.TimerPopupWearer,
            ("seconds", secondsLeft));

        _popup.PopupEntity(
            othersMsg,
            wearer,
            Filter.PvsExcept(wearer),
            recordReplay: true,
            type: PopupType.LargeCaution);

        _popup.PopupEntity(selfMsg, wearer, wearer, PopupType.LargeCaution);
        _audio.PlayPvs(clothing.WarningSound, itemUid);
    }

    private void TriggerExplosion(EntityUid itemUid, DnaLockClothingActiveComponent active, DnaLockClothingComponent clothing)
    {
        var wearer = active.WearerUid;

        if (!TerminatingOrDeleted(active.WearerUid))
        {
            var detonMsg = Loc.GetString("dna-lock-clothing-detonating");
            _popup.PopupEntity(detonMsg, wearer, Filter.Pvs(wearer), recordReplay: true, type: PopupType.LargeCaution);
        }

        var epicenterUid = !TerminatingOrDeleted(active.WearerUid) ? wearer : itemUid;

        _explosion.QueueExplosion(
            epicenterUid,
            clothing.ExplosionType,
            clothing.ExplosionTotalIntensity,
            clothing.ExplosionSlope,
            clothing.ExplosionMaxTileIntensity,
            tileBreakScale: 0f,
            maxTileBreak: 0,
            canCreateVacuum: false);

        QueueDel(itemUid);
    }
}
