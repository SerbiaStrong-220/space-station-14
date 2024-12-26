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
                _menu?.UpdateOperations(GetAvailableOperations(EntMan.GetEntity(update.User),
                                                                EntMan.GetEntity(update.Target)));
                break;
        }
    }

    private List<SurgeryGraphPrototype> GetAvailableOperations(EntityUid user, EntityUid target)
    {

        // soo...
        // For what it needed: to exclude operations which is role specific (such as mindslavefix)
        // also think of unavailability of some operation (no mindslave or you cant resurrect alive and etc)

        // we kinda need to check roles of performer and components of target
        // also also I want to have an advanced operation firstly to cure immobility or blindness <- how to do it in common way?
        // Maybe I need interface like ISurgeryGraphCondition to make it obvious and adoptable <- true, soo true
        // for beginning its better to prohibit making invalid operations.

        // Performer shouldnt see surgery if he is not allowed
        var result = _prototypeManager.EnumeratePrototypes<SurgeryGraphPrototype>()
            .Where((proto) =>
            {
                foreach (var condition in proto.PerformerAvailabilityCondition)
                {
                    if (!condition.Condition(user, EntMan, out _))
                        return false;
                }
                return true;
            })
            .ToList();

        return result;
    }
}
