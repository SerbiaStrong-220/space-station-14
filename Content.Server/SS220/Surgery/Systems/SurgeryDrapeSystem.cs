// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Ui;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.Surgery.Systems;

public sealed class SurgeryDrapeSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();


    }

    public void UpdateUserInterface(EntityUid drape, EntityUid user, EntityUid target)
    {

        var state = new SurgeryDrapeUpdate(user, target);
        _userInterface.SetUiState(drape, SurgeryDrapeUiKey.Key, state);
    }
}
