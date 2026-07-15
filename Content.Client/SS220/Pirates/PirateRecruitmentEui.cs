using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.SS220.Pirates;
using JetBrains.Annotations;

namespace Content.Client.SS220.Pirates;

[UsedImplicitly]
public sealed class PirateRecruitmentEui : BaseEui
{
    private readonly PirateRecruitmentWindow _window = new();
    private bool _responded;
    private bool _closingFromServer;

    public PirateRecruitmentEui()
    {
        _window.Response += Respond;
        _window.OnClose += () =>
        {
            if (!_closingFromServer)
                Respond(false);
        };
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _closingFromServer = true;
        _window.Close();
    }

    private void Respond(bool accepted)
    {
        if (_responded)
            return;

        _responded = true;
        SendMessage(new PirateRecruitmentChoiceMessage(accepted));
        _window.Close();
    }
}
