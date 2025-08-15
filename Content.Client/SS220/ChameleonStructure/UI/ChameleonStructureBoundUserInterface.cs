// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.ChameleonStructure;

namespace Content.Client.SS220.ChameleonStructure.UI;

[UsedImplicitly]
public sealed class ChameleonStructureBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private readonly ChameleonStructureSystem _chameleon;

    [ViewVariables]
    private ChameleonStructureMenu? _menu;

    public ChameleonStructureBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _chameleon = EntMan.System<ChameleonStructureSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ChameleonStructureMenu>();
        _menu.OnIdSelected += OnIdSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ChameleonStructureBoundUserInterfaceState st)
            return;

        var targets = _chameleon.GetValidTargets();

        if (st.RequiredTag == null)
        {
            _menu?.UpdateState(targets, st.SelectedId);
            return;
        }

        var newTargets = new List<EntProtoId>();
        foreach (var target in targets)
        {
            if (string.IsNullOrEmpty(target))
                continue;

            if (!_proto.HasIndex(target))
                continue;

            newTargets.Add(target);
        }
        _menu?.UpdateState(newTargets, st.SelectedId);
    }

    private void OnIdSelected(string selectedId)
    {
        SendMessage(new ChameleonStructurePrototypeSelectedMessage(selectedId));
    }
}
