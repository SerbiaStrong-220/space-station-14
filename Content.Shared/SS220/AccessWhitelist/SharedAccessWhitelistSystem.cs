// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Doors;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.AccessWhitelist;

public sealed class SharedAccessWhitelistSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessWhitelistComponent, DoorAccesAttemptEvent>(OnDoorAccesAttempt);
    }

    private void OnDoorAccesAttempt(Entity<AccessWhitelistComponent> ent, ref DoorAccesAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!CheckAccess(ent, args.User))
            args.Cancelled = true;
    }

    public bool CheckAccess(Entity<AccessWhitelistComponent> ent, EntityUid? user)
    {
        if (user == null)
            return false;

        if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, user.Value))
            return false;

        return true;
    }
}
