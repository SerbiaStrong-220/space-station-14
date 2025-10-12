// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Cargo.UI;
using Content.Client.UserInterface.Controls;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Events;
using Content.Shared.Ghost;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Client.UserInterface;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.SS220.CultYogg.MiGo.UI;

public sealed class MiGoTeleportBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private MiGoTeleportMenu? _menu;
    private readonly EntityUid _owner = owner;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindowCenteredLeft<MiGoTeleportMenu>();
        _menu.OnTeleportToTarget += TeleportToTarget;
        _menu.OnSpectate += Spectate;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MiGoTeleportBuiState cState)
            return;

        _menu?.Update(_owner, cState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_menu == null)
            return;

        _menu.OnClose -= Close;
    }

    private void TeleportToTarget(ButtonEventArgs args)
    {
        if (args.Button.Parent?.Parent?.Parent is not MiGoTeleportTarget target || target.TargetNetEnt == null)
            return;

        SendMessage(new MiGoTeleportToTargetMessage(target.TargetNetEnt));
    }

    private void Spectate(ButtonEventArgs args)
    {
        if (args.Button.Parent?.Parent?.Parent is not MiGoTeleportTarget target || target.TargetNetEnt == null)
            return;

        SendMessage(new MiGoSpectateMessage(target.TargetNetEnt));
    }
}
