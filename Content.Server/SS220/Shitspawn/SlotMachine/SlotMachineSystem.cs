using Content.Shared.SS220.Shitspawn.SlotMachine;
using Content.Shared.Stacks;
using Content.Shared.UserInterface;
using Content.Server.Stack;
using Content.Server.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.SS220.Shitspawn.SlotMachine;

public sealed class SlotMachineSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan SpinDuration = TimeSpan.FromSeconds(2.5);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlotMachineComponent, AfterActivatableUIOpenEvent>(OnAfterUIOpen);

        Subs.BuiEvents<SlotMachineComponent>(SlotMachineUiKey.Key,
            subs =>
            {
                subs.Event<SlotMachineSpinMessage>(OnSpin);
                subs.Event<SlotMachineInsertMessage>(OnInsert);
                subs.Event<SlotMachineCollectMessage>(OnCollect);
            });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SlotMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.HasPendingResult || now < comp.SpinEndTime)
                continue;

            comp.HasPendingResult = false;
            comp.Reels = comp.PendingReels;
            comp.IsWin = comp.PendingIsWin;
            comp.WinText = comp.PendingWinText;
            comp.LastPayout = comp.PendingPayout;
            comp.StoredCredits += comp.PendingPayout;
            Dirty(uid, comp);

            if (comp.IsWin)
                _audio.PlayPvs(comp.WinSound, uid);

            UpdateUI(uid, comp, spinning: false);
        }
    }

    private void OnAfterUIOpen(Entity<SlotMachineComponent> entity, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(entity.Owner, entity.Comp, spinning: entity.Comp.HasPendingResult);
    }

    private void OnInsert(Entity<SlotMachineComponent> entity, ref SlotMachineInsertMessage args)
    {
        if (entity.Comp.HasPendingResult)
        {
            _popup.PopupEntity(Loc.GetString("slot-machine-popup-spinning"), args.Actor, args.Actor);
            return;
        }

        _hands.TryGetActiveItem(args.Actor, out var item);

        if (!TryComp<StackComponent>(item, out var stack) || stack.StackTypeId != SlotMachineComponent.CreditStackId)
        {
            _popup.PopupEntity(Loc.GetString("slot-machine-popup-no-credits"), args.Actor, args.Actor);
            return;
        }

        var amountToTake = Math.Min(args.Amount, stack.Count);
        if (!_stack.Use(item.Value, amountToTake, stack))
            return;

        entity.Comp.StoredCredits += amountToTake;
        Dirty(entity.Owner, entity.Comp);
        _audio.PlayPvs(entity.Comp.InsertSound, entity.Owner);
        _popup.PopupEntity(Loc.GetString("slot-machine-popup-inserted", ("amount", amountToTake)),
            args.Actor,
            args.Actor);
        UpdateUI(entity.Owner, entity.Comp, spinning: false);
    }

    private void OnSpin(Entity<SlotMachineComponent> entity, ref SlotMachineSpinMessage args)
    {
        if (entity.Comp.HasPendingResult)
            return;

        var bet = Math.Max(SlotMachineComponent.MinBet, args.Bet);

        if (entity.Comp.StoredCredits < bet)
        {
            _popup.PopupEntity(Loc.GetString("slot-machine-popup-no-funds"), args.Actor, args.Actor);
            return;
        }

        entity.Comp.StoredCredits -= bet;
        entity.Comp.LastBet = bet;
        Dirty(entity.Owner, entity.Comp);

        var reels = new List<string>();
        foreach (var pool in entity.Comp.ReelPools)
        {
            var weightedSymbols = new List<string>();
            foreach (var symbol in pool.Symbols)
            {
                for (int i = 0; i < symbol.Weight; i++)
                {
                    weightedSymbols.Add(symbol.Id);
                }
            }

            reels.Add(_random.Pick(weightedSymbols));
        }

        var (isWin, winText, payout) = CalculateResult(entity.Comp, reels, bet);

        entity.Comp.PendingReels = reels;
        entity.Comp.PendingIsWin = isWin;
        entity.Comp.PendingWinText = winText;
        entity.Comp.PendingPayout = payout;
        entity.Comp.HasPendingResult = true;
        entity.Comp.SpinEndTime = _timing.CurTime + SpinDuration;

        _audio.PlayPvs(entity.Comp.SpinSound, entity.Owner);
        UpdateUI(entity.Owner, entity.Comp, spinning: true);
    }

    private void OnCollect(Entity<SlotMachineComponent> entity, ref SlotMachineCollectMessage args)
    {
        if (entity.Comp.HasPendingResult)
        {
            _popup.PopupEntity(Loc.GetString("slot-machine-popup-spinning"), args.Actor, args.Actor);
            return;
        }

        if (entity.Comp.StoredCredits <= 0)
            return;

        var money = Spawn(SlotMachineComponent.CashPrototypeId, Transform(entity.Owner).Coordinates);
        if (TryComp<StackComponent>(money, out var stack))
            _stack.SetCount(money, entity.Comp.StoredCredits, stack);

        _popup.PopupEntity(Loc.GetString("slot-machine-popup-collected", ("amount", entity.Comp.StoredCredits)),
            args.Actor,
            args.Actor);
        entity.Comp.StoredCredits = 0;
        Dirty(entity.Owner, entity.Comp);
        UpdateUI(entity.Owner, entity.Comp, spinning: false);
    }

    private void UpdateUI(EntityUid uid, SlotMachineComponent comp, bool spinning)
    {
        _uiSystem.SetUiState(uid,
            SlotMachineUiKey.Key,
            new SlotMachineBoundUserInterfaceState(
                comp.Reels,
                comp.StoredCredits,
                comp.IsWin,
                comp.WinText,
                comp.LastBet,
                comp.LastPayout,
                spinning,
                comp.Rules,
                comp.ReelPools));
    }

    private (bool isWin, string winText, int payout) CalculateResult(SlotMachineComponent comp,
        List<string> reels,
        int bet)
    {
        foreach (var rule in comp.Rules)
        {
            if (rule.Symbols == null || rule.Symbols.Count == 0)
                continue;

            bool match = true;
            if (rule.Index.HasValue)
            {
                var idx = rule.Index.Value;
                if (idx < 0 || idx + rule.Symbols.Count > reels.Count)
                    continue;

                for (var i = 0; i < rule.Symbols.Count; i++)
                {
                    if (reels[idx + i] != rule.Symbols[i])
                    {
                        match = false;
                        break;
                    }
                }
            }
            else
            {
                if (rule.Symbols.Count != reels.Count)
                    continue;

                for (var i = 0; i < reels.Count; i++)
                {
                    if (reels[i] != rule.Symbols[i])
                    {
                        match = false;
                        break;
                    }
                }
            }

            if (match)
                return (true, rule.WinText, bet * rule.Multiplier);
        }

        return (false, "", 0);
    }
}
