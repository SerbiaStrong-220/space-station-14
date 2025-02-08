using Content.Server.Nuke;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.LockPick;

public sealed class LockpickSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LockpickComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, LockpickComponent component, AfterInteractEvent args)
    {
        //change if need to lockpicking other items
        if (args.Target == null || !TryComp<NukeComponent>(args.Target.Value, out var nukeComp))
            return;

        if (!_tagSystem.HasTag(args.Target.Value, "CanLockPick"))
        {
            return;
        }

        var xform = Transform(args.Target.Value);

        if (xform.Anchored)
        {
            _transform.Unanchor(args.Target.Value, xform);
            _popups.PopupEntity(Loc.GetString("nuke-unanchored-with-lockpick"), args.Target.Value, args.User, PopupType.Medium);
        }
        else
        {
            _popups.PopupEntity(Loc.GetString("nuke-already-unanchored"), args.Target.Value, args.User, PopupType.MediumCaution);
        }
    }
}

