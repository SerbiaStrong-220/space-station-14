// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Access.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.SS220.CentComm;

public abstract class SharedCentCommEyeConsoleSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CentCommEyeConsoleComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<CentCommEyeComponent, InteractionAttemptEvent>(OnEyeInteractionAttempt);
    }

    /// <summary>
    /// Handles in-world console interaction.
    /// </summary>
    private void OnActivate(Entity<CentCommEyeConsoleComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!_mobState.IsAlive(args.User))
            return;

        args.Handled = true;

        if (!_power.IsPowered(ent.Owner))
        {
            _popup.PopupClient(Loc.GetString("centcomm-eye-console-unpowered"), ent, args.User);
            return;
        }

        if (!_access.IsAllowed(args.User, ent))
        {
            _popup.PopupClient(Loc.GetString("centcomm-eye-console-access-denied"), ent, args.User);
            return;
        }

        if (ent.Comp.User != null)
        {
            _popup.PopupClient(Loc.GetString("centcomm-eye-console-in-use"), ent, args.User);
            return;
        }

        StartObserving(ent, args.User);
    }

    protected virtual void StartObserving(Entity<CentCommEyeConsoleComponent> ent, EntityUid user)
    {
    }

    private void OnEyeInteractionAttempt(Entity<CentCommEyeComponent> ent, ref InteractionAttemptEvent args)
    {
        // Cancel everything but the eye's own Actions
        if (args.Target == ent.Owner)
            return;

        args.Cancelled = true;
    }
}
