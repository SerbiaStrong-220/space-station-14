using Content.Server.CartridgeLoader;
using Content.Server.GameTicking;
using Content.Shared.CartridgeLoader;
using Content.Shared.Interaction;
using Content.Shared.SS220.Cartridges.Timer;

namespace Content.Server.SS220.Cartridge.Timer;

public sealed partial class TimerCartridgeSystem : SharedTimerCartridgeSystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimerCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);
        SubscribeLocalEvent<TimerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<TimerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);

        SubscribeLocalEvent<TimerCartridgeInteractionComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<TimerCartridgeInteractionComponent> ent, ref InteractUsingEvent args)
    {
        if (!_cartridgeLoader.TryGetProgram<TimerCartridgeComponent>(ent.Owner, out var uid, out var program))
        {
            return;
        }

        UpdateUiState((uid.Value, program), ent.Owner);
    }

    public override void EndTimer(EntityUid uid, bool notify = true, TimerCartridgeComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Timer = TimeSpan.Zero;
        comp.TimerActive = false;

        if (notify && comp.TimerNotify)
        {
            if (Comp<CartridgeComponent>(uid).LoaderUid is not { } loaderUid)
                return;

            _cartridgeLoader.SendNotification(loaderUid, Loc.GetString("timer-cartridge-notification-header"), Loc.GetString("timer-cartridge-notification"));
        }
    }

    private void OnCartridgeRemoved(Entity<TimerCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        if (!_cartridgeLoader.HasProgram<TimerCartridgeComponent>(args.Loader))
        {
            RemComp<TimerCartridgeInteractionComponent>(args.Loader);
        }
    }

    private void OnUiReady(Entity<TimerCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(ent, args.Loader);
    }

    private void UpdateUiState(Entity<TimerCartridgeComponent> ent, EntityUid loaderUid)
    {
        var state = new TimerCartridgeUiState(
            date: DateTime.Now.AddYears(544), // note: add 544 years due to the lore
            shiftLength: _ticker.RoundDuration(),
            timerNotify: ent.Comp.TimerNotify,
            timer: ent.Comp.Timer,
            timerActive: ent.Comp.TimerActive
        );
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    private void OnUiMessage(Entity<TimerCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not TimerCartridgeUiMessageEvent message)
            return;

        var timer = ent.Comp;

        switch (message.Action)
        {
            case TimerCartridgeUiAction.EnableTimer:
                if (message.Timer < TimeSpan.Zero)
                    return;

                timer.Timer = message.Timer;
                timer.TimerActive = true;

                break;
            case TimerCartridgeUiAction.DisableTimer:
                timer.TimerActive = false;

                break;
            case TimerCartridgeUiAction.ToggleNotification:
                timer.TimerNotify = message.TimerNotify;

                break;
        }

        UpdateUiState(ent, GetEntity(args.LoaderUid));
    }
}
