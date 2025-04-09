// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Ui;
using Content.Shared.SS220.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.Surgery.Systems;

public sealed class SurgeryDrapeSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryDrapeComponent, AfterInteractUIOpenEvent>(AfterOpenUI);
        SubscribeLocalEvent<SurgeryDrapeComponent, InteractUITargetUpdate>(TargetUpdate);
    }

    private void AfterOpenUI(Entity<SurgeryDrapeComponent> entity, ref AfterInteractUIOpenEvent args)
    {
        UpdateUserInterface(entity.Owner, args.User, args.Target);
    }

    private void TargetUpdate(Entity<SurgeryDrapeComponent> entity, ref InteractUITargetUpdate args)
    {
        UpdateUserInterface(entity.Owner, args.User, args.Target);
    }

    public void UpdateUserInterface(EntityUid drape, EntityUid user, EntityUid target)
    {
        var netUser = GetNetEntity(user);
        var netTarget = GetNetEntity(target);

        var state = new SurgeryDrapeUpdate(netUser, netTarget);
        _userInterface.SetUiState(drape, SurgeryDrapeUiKey.Key, state);
    }
}
