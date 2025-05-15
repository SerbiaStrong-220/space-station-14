// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Tag;
using Content.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.ChameleonStructure;
using Content.Client.SS220.ChameleonStructure;
using Content.SS220.ChameleonStructure.UI;

namespace Content.Client.SS220.ChameleonStructure.UI;

[UsedImplicitly]
public sealed class ChameleonStructureBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private readonly ChameleonStructureSystem _chameleon;
    private readonly TagSystem _tag;

    [ViewVariables]
    private ChameleonStructureMenu? _menu;

    public ChameleonStructureBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _chameleon = EntMan.System<ChameleonStructureSystem>();
        _tag = EntMan.System<TagSystem>();
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
    }

    private void OnIdSelected(string selectedId)
    {
        SendMessage(new ChameleonStructurePrototypeSelectedMessage(selectedId));
    }
}
