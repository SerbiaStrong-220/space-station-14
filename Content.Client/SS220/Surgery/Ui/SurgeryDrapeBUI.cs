// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Ui;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Surgery.Ui;

public sealed class SurgeryDrapeBUI : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [ViewVariables]
    private SurgeryDrapeMenu? _menu;

    public SurgeryDrapeBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<SurgeryDrapeMenu>();

        _menu.OnSurgeryConfirmCLicked += (id, target) =>
        {
            var user = EntMan.GetNetEntity(_playerManager.LocalEntity);
            SendMessage(new StartSurgeryMessage(id, EntMan.GetNetEntity(target), user));

            if (user == null)
                return;

            var ev = new StartSurgeryEvent(id, EntMan.GetNetEntity(target), user.Value);
            EntMan.EventBus.RaiseLocalEvent(Owner, ev);

            if (!ev.Cancelled)
                this.Close();
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

    private List<SurgeryGraphPrototype> GetAvailableOperations(EntityUid user, EntityUid target)
    {
        // Performer shouldnt see surgery if he is not allowed
        var result = _prototypeManager.EnumeratePrototypes<SurgeryGraphPrototype>()
            .Where((graph) =>
            {
                return SharedSurgeryAvaibilityChecks.IsSurgeryGraphAvailablePerformer(user, graph, EntMan);
            })
            .ToList();

        return result;
    }
}
