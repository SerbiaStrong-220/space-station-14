using Content.Shared.Anomaly;
using Content.Shared.SS220.Anomaly;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Anomaly.Ui;

[UsedImplicitly]
public sealed class AnomalyGeneratorBoundUserInterface : BoundUserInterface
{
    private AnomalyGeneratorWindow? _window;

    public AnomalyGeneratorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AnomalyGeneratorWindow>();
        _window.SetEntity(Owner);

        _window.OnGenerateButtonPressed += () =>
        {
            SendMessage(new AnomalyGeneratorGenerateButtonPressedEvent());
        };

        //ss220 add anomaly place start
        _window.OnChooseAnomalyPlace += beacon =>
        {
            var msg = new AnomalyGeneratorChooseAnomalyPlaceMessage(beacon);
            SendMessage(msg);
        };
        //ss220 add anomaly place end
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        //ss220 add anomaly place start
        if (state is AnomalyGeneratorUserInterfaceState msg)
            _window?.UpdateState(msg);
        //ss220 add anomaly place end
    }

    //ss220 add anomaly place start
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is AnomalyGeneratorEmaggedEventMessage emagMessage)
            _window?.UpdateEmagState(emagMessage);
        //ss220 add anomaly place end
    }
}

