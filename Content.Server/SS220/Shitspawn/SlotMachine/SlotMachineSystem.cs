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

    private readonly string[] _reel1 =
    {
        "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry",
        "apple", "apple", "apple", "apple", "apple", "apple", "apple",
        "cherry", "cherry", "cherry", "cherry", "cherry", "cherry",
        "bell", "bell", "bell", "bell", "bell",
        "seven", "seven", "seven", "seven", "seven",
        "diamond", "diamond", "diamond",
    };

    private readonly string[] _reel2 =
    {
        "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry",
        "apple", "apple", "apple", "apple", "apple", "apple", "apple",
        "cherry", "cherry", "cherry", "cherry", "cherry", "cherry",
        "bell", "bell", "bell", "bell",
        "seven", "seven", "seven",
        "diamond", "diamond",
    };

    private readonly string[] _reel3 =
    {
        "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry", "blue_cherry",
        "apple", "apple", "apple", "apple", "apple", "apple", "apple",
        "cherry", "cherry", "cherry", "cherry", "cherry",
        "bell", "bell", "bell", "bell",
        "seven", "seven", "seven",
        "diamond",
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlotMachineComponent, AfterActivatableUIOpenEvent>(OnAfterUIOpen);

        Subs.BuiEvents<SlotMachineComponent>(SlotMachineUiKey.Key, subs =>
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
            comp.LastResult = comp.PendingResult;
            comp.LastPayout = comp.PendingPayout;
            comp.StoredCredits += comp.PendingPayout;
            Dirty(uid, comp);

            if (comp.LastResult != SlotMachineResult.Lose)
                _audio.PlayPvs(comp.WinSound, uid);

            UpdateUI(uid, comp, spinning: false);
        }
    }

    private void OnAfterUIOpen(EntityUid uid, SlotMachineComponent comp, AfterActivatableUIOpenEvent args)
    {
        UpdateUI(uid, comp, spinning: comp.HasPendingResult);
    }

    private void OnInsert(EntityUid uid, SlotMachineComponent comp, SlotMachineInsertMessage args)
    {
        if (comp.HasPendingResult)
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
        if (!_stack.TryUse(item.Value, amountToTake))
            return;

        comp.StoredCredits += amountToTake;
        Dirty(uid, comp);
        _audio.PlayPvs(comp.InsertSound, uid);
        _popup.PopupEntity(Loc.GetString("slot-machine-popup-inserted", ("amount", amountToTake)), args.Actor, args.Actor);
        UpdateUI(uid, comp, spinning: false);
    }

    private void OnSpin(EntityUid uid, SlotMachineComponent comp, SlotMachineSpinMessage args)
    {
        if (comp.HasPendingResult)
            return;

        var bet = Math.Max(SlotMachineComponent.MinBet, args.Bet);

        if (comp.StoredCredits < bet)
        {
            _popup.PopupEntity(Loc.GetString("slot-machine-popup-no-funds"), args.Actor, args.Actor);
            return;
        }

        comp.StoredCredits -= bet;
        comp.LastBet = bet;
        Dirty(uid, comp);

        var reels = new[]
        {
            _random.Pick(_reel1),
            _random.Pick(_reel2),
            _random.Pick(_reel3),
        };

        var (result, payout) = CalculateResult(reels, bet);

        comp.PendingReels = reels;
        comp.PendingResult = result;
        comp.PendingPayout = payout;
        comp.HasPendingResult = true;
        comp.SpinEndTime = _timing.CurTime + SpinDuration;

        _audio.PlayPvs(comp.SpinSound, uid);
        UpdateUI(uid, comp, spinning: true);
    }

    private void OnCollect(EntityUid uid, SlotMachineComponent comp, SlotMachineCollectMessage args)
    {
        if (comp.HasPendingResult)
        {
            _popup.PopupEntity(Loc.GetString("slot-machine-popup-spinning"), args.Actor, args.Actor);
            return;
        }

        if (comp.StoredCredits <= 0)
            return;

        var money = Spawn(SlotMachineComponent.CashPrototypeId, Transform(uid).Coordinates);
        if (TryComp<StackComponent>(money, out var stack))
            _stack.SetCount(money, comp.StoredCredits, stack);

        _popup.PopupEntity(Loc.GetString("slot-machine-popup-collected", ("amount", comp.StoredCredits)), args.Actor, args.Actor);
        comp.StoredCredits = 0;
        Dirty(uid, comp);
        UpdateUI(uid, comp, spinning: false);
    }

    private void UpdateUI(EntityUid uid, SlotMachineComponent comp, bool spinning)
    {
        _uiSystem.SetUiState(uid, SlotMachineUiKey.Key,
            new SlotMachineBoundUserInterfaceState(
                comp.Reels, comp.StoredCredits,
                spinning ? SlotMachineResult.None : comp.LastResult,
                comp.LastBet, comp.LastPayout, spinning));
    }

    private (SlotMachineResult result, int payout) CalculateResult(string[] reels, int bet)
    {
        if (reels[0] == reels[1] && reels[1] == reels[2])
        {
            var mult = reels[0] switch
            {
                "diamond" => 100,
                "seven"   => 50,
                "bell"    => 30,
                "cherry"  => 20,
                "apple"   => 15,
                _         => 10,
            };
            var result = reels[0] switch
            {
                "diamond" => SlotMachineResult.Jackpot,
                "seven"   => SlotMachineResult.Triple7,
                _         => SlotMachineResult.Triple,
            };
            return (result, bet * mult);
        }

        if (reels[0] == "apple" && reels[1] == "apple" && reels[2] != "apple")
            return (SlotMachineResult.ApplePair, bet * 2);

        if (reels[1] == "cherry" && reels[2] == "cherry" && reels[0] != "cherry")
            return (SlotMachineResult.CherryPair, bet * 2);

        return (SlotMachineResult.Lose, 0);
    }
}
