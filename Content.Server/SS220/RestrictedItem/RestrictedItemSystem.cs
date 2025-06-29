// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction.Events;
using Content.Shared.SS220.RestrictedItem;

namespace Content.Server.SS220.RestrictedItem;

public sealed partial class RestrictedItemSystem : SharedRestrictedItemSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RestrictedItemComponent, GettingUsedAttemptEvent>(OnUseAttempt);
    }

    private void OnUseAttempt(Entity<RestrictedItemComponent> ent, ref GettingUsedAttemptEvent args)
    {
        if (!CanInteract(args.User, ent))
            args.Cancel();
    }
}
