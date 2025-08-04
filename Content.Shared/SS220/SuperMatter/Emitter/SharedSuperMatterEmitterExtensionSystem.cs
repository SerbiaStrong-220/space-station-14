// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.SuperMatter.Emitter;

public abstract class SharedSuperMatterEmitterExtensionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuperMatterEmitterExtensionComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUiAttempt);
    }

    private void OnActivatableUiAttempt(Entity<SuperMatterEmitterExtensionComponent> entity, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!CanInteract(entity, args.User, out var reason))
        {
            _popup.PopupClient(reason, entity, args.User);
            args.Cancel();
            return;
        }
    }

    protected bool CanInteract(Entity<SuperMatterEmitterExtensionComponent> emitter, EntityUid user, [NotNullWhen(false)] out string? reason)
    {
        reason = null;
        if (TryComp<LockComponent>(emitter, out var lockComp) && lockComp.Locked)
        {
            reason = Loc.GetString("supermatter-emitter-extension-locked-emitter");
            return false;
        }

        return true;
    }
}
