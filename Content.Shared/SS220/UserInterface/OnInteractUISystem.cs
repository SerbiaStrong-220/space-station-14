// Highly inspired by ActivatableUISystem all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;

namespace Content.Shared.SS220.UserInterface;

public sealed class OnInteractUsingSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OnInteractUIComponent, AfterInteractEvent>(InteractUsing);
    }

    private void InteractUsing(Entity<OnInteractUIComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        args.Handled = InteractUI(args.User, entity, args.Target.Value);
    }

    private bool InteractUI(EntityUid user, Entity<OnInteractUIComponent> uiEntity, EntityUid target)
    {
        if (uiEntity.Comp.Key == null || !_userInterface.HasUi(uiEntity.Owner, uiEntity.Comp.Key))
            return false;

        if (_userInterface.IsUiOpen(uiEntity.Owner, uiEntity.Comp.Key, user))
        {
            _userInterface.CloseUi(uiEntity.Owner, uiEntity.Comp.Key, user);
            return true;
        }

        if (!_actionBlocker.CanInteract(user, uiEntity.Owner))
            return false;

        // Soo. Cant do anything better - good job!
        // If we've gotten this far, fire a cancellable event that indicates someone is about to activate this.
        // This is so that stuff can require further conditions (like power).
        var oie = new InteractUIOpenAttemptEvent(user);
        var uie = new UserOpenInteractUIAttemptEvent(user, uiEntity);
        RaiseLocalEvent(user, uie);
        RaiseLocalEvent(uiEntity, oie);
        if (oie.Cancelled || uie.Cancelled)
            return false;

        // Give the UI an opportunity to prepare itself if it needs to do anything
        // before opening
        var bie = new BeforeInteractUIOpenEvent(user, target);
        RaiseLocalEvent(uiEntity, bie);

        _userInterface.OpenUi(uiEntity.Owner, uiEntity.Comp.Key, user);

        //Let the component know a user opened it so it can do whatever it needs to do
        var aie = new AfterInteractUIOpenEvent(user, target, user);
        RaiseLocalEvent(uiEntity, aie);

        return true;
    }
}
