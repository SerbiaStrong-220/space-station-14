// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction.Events;
using Content.Shared.SS220.RoleItem;

namespace Content.Server.SS220.RoleItem;

public sealed partial class RoleItemSystem : SharedRoleItemSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoleItemComponent, BeingUsedAttemptEvent>(OnUseAttempt);
    }

    private void OnUseAttempt(Entity<RoleItemComponent> ent, ref BeingUsedAttemptEvent args)
    {
        if (!ItemCheck(args.Uid, ent))
            args.Cancel();
    }
}
