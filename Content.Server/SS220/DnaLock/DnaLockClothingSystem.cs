// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Explosion.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.Popups;
using Content.Shared.SS220.DnaLock;
using Content.Shared.SS220.DnaLock.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DnaLock;

public sealed partial class DnaLockClothingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private DnaLockSystem _dnaLock = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(ExplosionSystem));

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
                RemCompDeferred<DnaLockClothingActiveComponent>(uid);
                continue;
            }

            if (curTime >= active.ExplodeAt)
            {
                Detonate(uid, active.WearerUid, clothing);
                continue;
            }

            if (curTime < active.NextBeepAt)
                continue;

            active.NextBeepAt += clothing.BeepInterval;
            var secondsLeft = (int) Math.Ceiling((active.ExplodeAt - curTime).TotalSeconds);
            Warn(uid, active.WearerUid, clothing, clothing.TimerPopup, secondsLeft);
        }
    }

    private void OnEquipped(Entity<DnaLockClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!TryComp<DnaLockableComponent>(ent, out var dnaLock)
            || _dnaLock.CheckAccess((ent, dnaLock), args.Wearer, silentFail: true))
            return;

        var curTime = _timing.CurTime;
        var active = EnsureComp<DnaLockClothingActiveComponent>(ent);
        active.WearerUid = args.Wearer;
        active.ExplodeAt = curTime + ent.Comp.TimeToExplode;
        active.NextBeepAt = curTime + ent.Comp.BeepInterval;

        var secondsLeft = (int) Math.Ceiling(ent.Comp.TimeToExplode.TotalSeconds);
        Warn(ent, args.Wearer, ent.Comp, ent.Comp.WarningPopup, secondsLeft);
    }

    private void OnUnequipped(Entity<DnaLockClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemComp<DnaLockClothingActiveComponent>(ent);
    }

    private void Warn(EntityUid item, EntityUid wearer, DnaLockClothingComponent clothing, LocId message, int secondsLeft)
    {
        _popup.PopupEntity(Loc.GetString(message, ("seconds", secondsLeft)), wearer, PopupType.LargeCaution);
        _audio.PlayPvs(clothing.WarningSound, item);
    }

    private void Detonate(EntityUid item, EntityUid wearer, DnaLockClothingComponent clothing)
    {
        _popup.PopupEntity(Loc.GetString(clothing.DetonationPopup), wearer, PopupType.LargeCaution);
        _explosion.TriggerExplosive(item, user: wearer);
    }
}
