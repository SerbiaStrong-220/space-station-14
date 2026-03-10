using Content.Client.UserInterface.Controls;
using Content.Shared.SS220.BeerUpdate.MaltGrinder;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.SS220.BeerUpdate.UI;

public sealed class MaltGrinderBoundUserInterface : BoundUserInterface
{
    private SimpleRadialMenu? _menu;

    public MaltGrinderBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);

        var buttons = new List<RadialMenuOptionBase>
        {
            new RadialMenuActionOption<bool>(OnStartPressed, true)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(
                    new SpriteSpecifier.Texture(new ResPath("/Textures/SS220/Interface/Radial/radial_power.png"))
                ),
                ToolTip = Loc.GetString("malt-grinder-start-button")
            },

            new RadialMenuActionOption<bool>(OnEjectPressed, true)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(
                    new SpriteSpecifier.Texture(new ResPath("/Textures/SS220/Interface/Radial/radial_eject.png"))
                ),
                ToolTip = Loc.GetString("malt-grinder-eject-button")
            }
        };

        _menu.SetButtons(buttons);
        _menu.OpenOverMouseScreenPosition();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not MaltGrinderInterfaceState maltState)
            return;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
    }

    private void OnStartPressed(bool data)
    {
        SendMessage(new MaltGrinderStartMessage());
    }

    private void OnEjectPressed(bool data)
    {
        SendMessage(new MaltGrinderEjectChamberAllMessage());
    }
}
