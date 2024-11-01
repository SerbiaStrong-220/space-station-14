// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Popups;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Server.SS220.MindSlave.DisfunctionSystem;

public sealed class WieldUnabilitySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WieldableComponent, BeforeWieldEvent>(OnWieldAttempt);
    }

    private void OnWieldAttempt(Entity<WieldableComponent> entity, ref BeforeWieldEvent args)
    {
        _popup.PopupClient(Loc.GetString("unable-to-wield"), entity, type: Shared.Popups.PopupType.MediumCaution);
        args.Cancel();
    }
}
