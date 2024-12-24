// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Ui;
using Content.Shared.Whitelist;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Surgery.Ui;

public sealed class SurgeryDrapeBUI : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;

    [ViewVariables]
    private SurgeryDrapeMenu? _menu;

    public SurgeryDrapeBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<SurgeryDrapeMenu>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case SurgeryDrapeUpdate update:
                _menu?.UpdateOperations(GetAvailableOperations(update.User, update.Target));
                break;
        }
    }

    private List<SurgeryGraphPrototype> GetAvailableOperations(EntityUid user, EntityUid target)
    {
        var result = _prototypeManager.EnumeratePrototypes<SurgeryGraphPrototype>()
            .Where(proto => _entityWhitelist.IsWhitelistPass(proto.TargetWhitelist, target)
                            && _entityWhitelist.IsWhitelistPass(proto.PerformerWhitelist, user))
            .ToList();

        return result;
    }
}
