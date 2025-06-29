// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SmartGasMask.Events;
using Content.Shared.SS220.SmartGasMask.Prototype;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.SmartGasMask;

public sealed class SmartGasMaskBoundUserInterface : BoundUserInterface
{
    private SmartGasMaskMenu? _smartGasMaskMenu;

    public SmartGasMaskBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _smartGasMaskMenu = this.CreateWindow<SmartGasMaskMenu>();
        _smartGasMaskMenu.SetEntity(Owner);
        _smartGasMaskMenu.SendAlertSmartGasMaskRadioMessageAction += SendAlertSmartGasMaskRadioMessage;
    }

    private void SendAlertSmartGasMaskRadioMessage(ProtoId<AlertSmartGasMaskPrototype> protoId)
    {
        SendMessage(new SmartGasMaskMessage(protoId));
    }
}
