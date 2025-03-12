// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Communications;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Content.Shared.SS220.CluwneCommunications;

namespace Content.Client.SS220.CluwneComms.UI
{
    public sealed class CluwneCommunicationsConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        [ViewVariables]
        private CluwneCommunicationsConsoleMenu? _menu;

        public CluwneCommunicationsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<CluwneCommunicationsConsoleMenu>();
            _menu.OnAnnounce += AnnounceButtonPressed;
        }

        public void AnnounceButtonPressed(string message)
        {
            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var msg = SharedChatSystem.SanitizeAnnouncement(message, maxLength);
            SendMessage(new CommunicationsConsoleAnnounceMessage(msg));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not CluwneCommunicationsConsoleInterfaceState commsState)
                return;

            if (_menu != null)
            {
                _menu.CanAnnounce = commsState.CanAnnounce;
            }
        }
    }
}
