using Content.Shared.Containers.ItemSlots;
using Content.Shared.SS220.PdaIdPainter;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.PdaIdPainter;

public sealed partial class PdaIdPainterBoundUserInterface : BoundUserInterface
{
    private PdaIdPainter? _window;

    public PdaIdPainterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        if (state is not PdaIdPainterBoundState args)
            return;

        var targetPda = EntMan.GetEntity(args.TargetPda);
        var targetId = EntMan.GetEntity(args.TargetId);

        _window.InsertIdButton.Text = args.TargetId.HasValue
            ? Loc.GetString("pda-id-painter-console-eject-button")
            : Loc.GetString("pda-id-painter-console-insert-button");

        _window.InsertPdaButton.Text = args.TargetPda.HasValue
            ? Loc.GetString("pda-id-painter-console-eject-button")
            : Loc.GetString("pda-id-painter-console-insert-button");
        UpdatePopulate(targetPda, targetId);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PdaIdPainter>();

        _window.OnPdaPicked = OnPdaPicked;
        _window.OnIdPicked = OnIdPicked;

        if (EntMan.TryGetComponent(Owner, out PdaIdPainterComponent? comp))
        {
            UpdatePopulate(comp.PdaSlot.Item, comp.IdCardSlot.Item);
        }

        _window.InsertIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PdaIdPainterComponent.IdPainterSlot));
        _window.InsertPdaButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PdaIdPainterComponent.PdaPainterSlot));
    }

    private void UpdatePopulate(EntityUid? targetPda, EntityUid? targetId)
    {
        EntProtoId? chosenPda = null;
        EntProtoId? chosenId = null;

        if (_window?.PdaAndIds == null)
            return;

        if (EntMan.TryGetComponent<PdaIdPainterTargetComponent>(targetId, out var idComp))
        {
            chosenId = idComp.NewProto;
        }
        else
        {
            if (EntMan.TryGetComponent(targetId, out MetaDataComponent? metaDataComponent) &&
                metaDataComponent.EntityPrototype != null)
            {
                chosenId = metaDataComponent.EntityPrototype.ID;
            }
        }

        if (EntMan.TryGetComponent<PdaIdPainterTargetComponent>(targetPda, out var pdaComp))
        {
            chosenPda = pdaComp.NewProto;
        }
        else
        {
            if (EntMan.TryGetComponent(targetPda, out MetaDataComponent? metaDataComponent) &&
                metaDataComponent.EntityPrototype != null)
            {
                chosenPda = metaDataComponent.EntityPrototype.ID;
            }
        }

        _window.Populate(EntMan.System<PdaIdPainterSystem>().PdaAndIdProtos, chosenPda, chosenId);
    }

    private void OnPdaPicked(string args)
    {
        SendMessage(new PdaIdPainterPickedPdaMessage(args));
    }

    private void OnIdPicked(string args)
    {
        SendMessage(new PdaIdPainterPickedIdMessage(args));
    }

}
