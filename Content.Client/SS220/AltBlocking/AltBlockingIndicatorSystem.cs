// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.SS220.AltBlocking;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.AltBlocking;

/// <summary>
///     Handles displaying SSD indicator as status icon
/// </summary>
public sealed class AltBLockingIndicatorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltBlockingUserComponent, GetStatusIconsEvent>(OnGetStatusIcon);
    }

    private void OnGetStatusIcon(Entity<AltBlockingUserComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.Icon, out var iconPrototype) && ent.Comp.Blocking)
            args.StatusIcons.Add(iconPrototype);
    }
}
