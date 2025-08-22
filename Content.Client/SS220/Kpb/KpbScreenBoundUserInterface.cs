using Content.Shared.Humanoid.Markings;
using Content.Shared.SS220.Kpb;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Kpb;

public sealed class KpbScreenBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private KpbScreenWindow? _window;

    public KpbScreenBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<KpbScreenWindow>();

        _window.OnFacialHairSelected += tuple => SelectHair(KpbScreenCategory.FacialHair, tuple.id, tuple.slot);
        _window.OnFacialHairColorChanged +=
            args => ChangeColor(KpbScreenCategory.FacialHair, args.marking, args.slot);
        _window.OnFacialHairSlotAdded += delegate () { AddSlot(KpbScreenCategory.FacialHair); };
        _window.OnFacialHairSlotRemoved += args => RemoveSlot(KpbScreenCategory.FacialHair, args);
    }

    private void SelectHair(KpbScreenCategory category, string marking, int slot)
    {
        SendMessage(new KpbScreenSelectMessage(category, marking, slot));
    }

    private void ChangeColor(KpbScreenCategory category, Marking marking, int slot)
    {
        SendMessage(new KpbScreenChangeColorMessage(category, new(marking.MarkingColors), slot));
    }

    private void RemoveSlot(KpbScreenCategory category, int slot)
    {
        SendMessage(new KpbScreenRemoveSlotMessage(category, slot));
    }

    private void AddSlot(KpbScreenCategory category)
    {
        SendMessage(new KpbScreenAddSlotMessage(category));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not KpbScreenUiState data || _window == null)
        {
            return;
        }

        _window.UpdateState(data);
    }
}
