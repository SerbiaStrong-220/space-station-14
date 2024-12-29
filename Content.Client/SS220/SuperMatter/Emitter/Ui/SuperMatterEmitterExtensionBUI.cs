// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.UserInterface;
using Content.Shared.SS220.SuperMatter.Ui;
using Content.Shared.SS220.SuperMatter.Emitter;

namespace Content.Client.SS220.SuperMatter.Emitter.Ui;

public sealed class SuperMatterEmitterExtensionBUI : BoundUserInterface
{
    [ViewVariables]
    private SuperMatterEmitterExtensionMenu? _menu;
    private int? _power;
    private int? _ratio;
    public SuperMatterEmitterExtensionBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }
    protected override void Open()
    {
        base.Open();
        if (EntMan.TryGetComponent<SuperMatterEmitterExtensionComponent>(Owner, out var superMatterEmitter))
        {
            _power = superMatterEmitter.PowerConsumption;
            _ratio = superMatterEmitter.EnergyToMatterRatio;
        }

        _menu = this.CreateWindow<SuperMatterEmitterExtensionMenu>();
        _menu.SetEmitterParams(_ratio, _power);
        _menu.OnSubmitButtonPressed += (_, powerConsumption, ratio) =>
        {
            SendMessage(new SuperMatterEmitterExtensionValueMessage(powerConsumption, ratio));
        };
    }
}
