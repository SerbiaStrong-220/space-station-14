// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Systems;
using Content.Shared.SS220.Surgery.Ui;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Surgery.SurgeryStartUi;

public sealed class SurgeryDrapeBUI : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly SharedSurgerySystem _surgery = default!;

    [ViewVariables]
    private SurgeryDrapeMenu? _menu;

    public SurgeryDrapeBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _surgery = IoCManager.Resolve<IEntityManager>().System<SharedSurgerySystem>();
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<SurgeryDrapeMenu>();

        _menu.Used = Owner;

        _menu.OnSurgeryConfirmClicked += (id, target) =>
        {
            var user = EntMan.GetNetEntity(_playerManager.LocalEntity);

            if (user == null)
                return;

            SendMessage(new StartSurgeryMessage(id, EntMan.GetNetEntity(target), user.Value, EntMan.GetNetEntity(Owner)));

            var ev = new StartSurgeryEvent(id, EntMan.GetNetEntity(target), user.Value, EntMan.GetNetEntity(Owner));
            EntMan.EventBus.RaiseLocalEvent(Owner, ev);

            if (!ev.Cancelled)
                Close();
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case SurgeryDrapeUpdate update:
                _menu?.UpdateTarget(EntMan.GetEntity(update.Target));
                _menu?.AddOperations(GetAvailableOperations(EntMan.GetEntity(update.User),
                                                                EntMan.GetEntity(update.Target)));
                break;
        }
    }

    private List<SurgeryStartInfo> GetAvailableOperations(EntityUid user, EntityUid target)
    {
        List<SurgeryStartInfo> result = new();
        foreach (var surgeryGraph in _prototypeManager.EnumeratePrototypes<SurgeryGraphPrototype>())
        {
            result.Add(new SurgeryStartInfo(surgeryGraph, _surgery.CanStartSurgery(user, surgeryGraph, target, Owner, out var reason), reason));
        }

        return result;
    }
}
