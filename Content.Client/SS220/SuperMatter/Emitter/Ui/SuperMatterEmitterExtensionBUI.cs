// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.UserInterface;
using Content.Shared.SS220.SuperMatter.Ui;
using Content.Shared.SS220.SuperMatter.Emitter;
using Content.Shared.Singularity.Components;

namespace Content.Client.SS220.SuperMatter.Emitter.Ui;

public sealed class SuperMatterEmitterExtensionBUI : BoundUserInterface
{
    [ViewVariables]
    private SuperMatterEmitterExtensionMenu? _menu;

    private int? _power;
    private int? _ratio;

    private bool _emitterActivated = false;

    public SuperMatterEmitterExtensionBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        if (EntMan.TryGetComponent<SuperMatterEmitterExtensionComponent>(Owner, out var superMatterEmitter))
        {
            _power = superMatterEmitter.PowerConsumption;
            _ratio = superMatterEmitter.EnergyToMatterRatio;
        }

        if (EntMan.TryGetComponent<EmitterComponent>(Owner, out var emitterComponent))
        {
            _emitterActivated = emitterComponent.IsOn;
        }

        _menu = this.CreateWindow<SuperMatterEmitterExtensionMenu>();
        _menu.SetEmitterParams(_ratio, _power);

        var state = _emitterActivated ? ActivationStateEnum.EmitterActivated : ActivationStateEnum.EmitterDeactivated;
        _menu.ChangeActivationState(state);

        _menu.OnSubmitButtonPressed += (_, powerConsumption, ratio) =>
        {
            SendMessage(new SuperMatterEmitterExtensionValueMessage(powerConsumption, ratio));
        };
        _menu.OnEmitterActivatePressed += (_) =>
        {
            SendMessage(new SuperMatterEmitterExtensionEmitterActivateMessage());

            var state = _emitterActivated ? ActivationStateEnum.EmitterDeactivated : ActivationStateEnum.EmitterActivated;
            _emitterActivated = !_emitterActivated;
            _menu.ChangeActivationState(state);
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case SuperMatterEmitterExtensionUpdate update:
                _menu?.SetEmitterParams(update.EnergyToMatterRatio, update.PowerConsumption);
                break;
        }
    }
}
