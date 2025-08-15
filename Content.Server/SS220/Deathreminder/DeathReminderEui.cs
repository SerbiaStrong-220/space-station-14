using Content.Server.EUI;
using Content.Shared.Cloning;
using Content.Shared.Eui;

namespace Content.Server.SS220.Deathreminder;

public sealed class DeathReminderEui : BaseEui
{
    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AcceptCloningChoiceMessage choice ||
            choice.Button == AcceptCloningUiButton.Deny)
        {
            Close();
            return;
        }

        Close();
    }
}
