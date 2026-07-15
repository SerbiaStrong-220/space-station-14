using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.SS220.Pirates;

namespace Content.Server.SS220.Pirates;

public sealed class PirateRecruitmentEui(EntityUid contract, EntityUid target, PirateRecruitmentSystem system) : BaseEui
{
    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not PirateRecruitmentChoiceMessage choice)
            return;

        if (Player.AttachedEntity != target)
        {
            Close();
            return;
        }

        system.RespondToOffer(contract, target, choice.Accepted);
        Close();
    }

    public override void Closed()
    {
        base.Closed();
        system.ClearOffer(contract, target);
    }
}
