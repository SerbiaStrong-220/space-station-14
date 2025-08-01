// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Lock;
using Content.Shared.Singularity.Components;
using Content.Shared.SS220.SuperMatter.Emitter;
using Content.Shared.SS220.SuperMatter.Ui;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.SuperMatter.Emitter.Ui;

public sealed class SuperMatterEmitterBUI : BoundUserInterface
{
    [ViewVariables]
    private SuperMatterEmitterMenu? _menu;

    private int? _power;
    private int? _ratio;

    private bool _emitterActivated = false;

    public SuperMatterEmitterBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        if (EntMan.TryGetComponent<LockComponent>(Owner, out var lockComp) && lockComp.Locked)
        {
            // If the emitter is locked, we don't open the UI.
            return;
        }


        if (EntMan.TryGetComponent<SuperMatterEmitterComponent>(Owner, out var superMatterEmitter))
        {
            _power = superMatterEmitter.PowerConsumption;
            _ratio = superMatterEmitter.EnergyToMatterRatio;

            _emitterActivated = superMatterEmitter.IsOn;
        }

        _menu = this.CreateWindow<SuperMatterEmitterMenu>();
        _menu.SetEmitterParams(_ratio, _power);

        var state = _emitterActivated ? ActivationStateEnum.EmitterActivated : ActivationStateEnum.EmitterDeactivated;
        _menu.ChangeActivationState(state);

        _menu.OnSubmitButtonPressed += (_, powerConsumption, ratio) =>
        {
            if (EntMan.TryGetComponent<LockComponent>(Owner, out var lockComp) && !lockComp.Locked)
            {
                SendMessage(new SuperMatterEmitterValueMessage(powerConsumption, ratio));
            }
        };
        _menu.OnEmitterActivatePressed += (_) =>
        {
            if (EntMan.TryGetComponent<LockComponent>(Owner, out var lockComp) && !lockComp.Locked)
            {
                SendMessage(new SuperMatterEmitterEmitterActivateMessage());

                var state = _emitterActivated ? ActivationStateEnum.EmitterDeactivated : ActivationStateEnum.EmitterActivated;
                _emitterActivated = !_emitterActivated;
                _menu.ChangeActivationState(state);
            }
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case SuperMatterEmitterUpdate update:
                _menu?.SetEmitterParams(update.EnergyToMatterRatio, update.PowerConsumption);
                break;
        }
    }
}
