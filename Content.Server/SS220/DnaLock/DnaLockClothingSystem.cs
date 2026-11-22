// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.SS220.DnaLock;
using Content.Shared.SS220.DnaLock.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DnaLock;

/// <summary>
/// Counts down and detonates DNA-locked clothing worn by someone without access.
/// </summary>
public sealed partial class DnaLockClothingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ClothingSystem _clothing = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private DnaLockSystem _dnaLock = default!;

    public override void Initialize()
    {
        base.Initialize();

        // The queued explosion is then processed on the next tick, after the item's queued deletion,
        // so the item's own explosion resistance doesn't shield its wearer from the blast.
        UpdatesAfter.Add(typeof(ExplosionSystem));

        SubscribeLocalEvent<DnaLockClothingComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<DnaLockClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<DnaLockClothingComponent, DnaLockActiveChangedEvent>(OnLockActiveChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<DnaLockClothingActiveComponent, DnaLockClothingComponent, DnaLockableComponent>();
        while (query.MoveNext(out var uid, out var active, out var clothing, out var dnaLock))
        {
            if (TerminatingOrDeleted(active.WearerUid))
            {
                RemCompDeferred<DnaLockClothingActiveComponent>(uid);
                continue;
            }

            if (_dnaLock.CheckAccess((uid, dnaLock), active.WearerUid, silentFail: true))
            {
                RemCompDeferred<DnaLockClothingActiveComponent>(uid);
                continue;
            }

            if (curTime >= active.ExplodeAt)
            {
                RemCompDeferred<DnaLockClothingActiveComponent>(uid);
                Detonate(uid, active.WearerUid);
                continue;
            }

            if (curTime < active.NextBeepAt)
                continue;

            // Counted from the previous beep rather than from now, so the beeps stay evenly spaced.
            active.NextBeepAt += clothing.BeepInterval;
            var secondsLeft = (int) Math.Ceiling((active.ExplodeAt - curTime).TotalSeconds);

            var message = secondsLeft <= 1 ? clothing.DetonationMessage : clothing.TimerMessage;
            Warn(uid, message, secondsLeft);
        }
    }

    private void OnEquipped(Entity<DnaLockClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        TryStartCountdown(ent, args.Wearer);
    }

    private void OnUnequipped(Entity<DnaLockClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemComp<DnaLockClothingActiveComponent>(ent);
    }

    // A lock turned on over clothing that is already worn, including binding it to someone else, must check the current wearer.
    private void OnLockActiveChanged(Entity<DnaLockClothingComponent> ent, ref DnaLockActiveChangedEvent args)
    {
        if (!args.Active
            || HasComp<DnaLockClothingActiveComponent>(ent)
            || !_clothing.IsEquipped(ent.Owner)
            || !_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        TryStartCountdown(ent, container.Owner);
    }

    private void TryStartCountdown(Entity<DnaLockClothingComponent> ent, EntityUid wearer)
    {
        if (!TryComp<DnaLockableComponent>(ent, out var dnaLock)
            || _dnaLock.CheckAccess((ent, dnaLock), wearer, silentFail: true))
            return;

        var curTime = _timing.CurTime;
        var active = EnsureComp<DnaLockClothingActiveComponent>(ent);
        active.WearerUid = wearer;
        active.ExplodeAt = curTime + ent.Comp.TimeToExplode;
        active.NextBeepAt = curTime + ent.Comp.BeepInterval;

        var secondsLeft = (int) Math.Ceiling(ent.Comp.TimeToExplode.TotalSeconds);
        Warn(ent, ent.Comp.WarningMessage, secondsLeft);
    }

    private void Warn(EntityUid item, LocId message, int secondsLeft)
    {
        Say(item, Loc.GetString(message, ("seconds", secondsLeft)));
    }

    private void Detonate(EntityUid item, EntityUid wearer)
    {
        // TriggerExplosive deletes the item by default.
        _explosion.TriggerExplosive(item, user: wearer);
    }

    private void Say(EntityUid item, string message)
    {
        _chat.TrySendInGameICMessage(item, message, InGameICChatType.Speak, hideChat: true, hideLog: true, checkRadioPrefix: false, ignoreActionBlocker: true);
    }
}
