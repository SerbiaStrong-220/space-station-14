// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Communications;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Content.Shared.SS220.CluwneComms;

namespace Content.Client.SS220.CluwneComms.UI
{
    public sealed class CluwneCommsConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        [ViewVariables]
        private CluwneCommsConsoleMenu? _menu;

        public CluwneCommsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<CluwneCommsConsoleMenu>();
            _menu.OnAnnounce += AnnounceButtonPressed;
            _menu.OnAlertLevel += AlertLevelSelected;
            _menu.OnAlert += AlertButtonPressed;
            _menu.OnBoom += BoomButtonPressed;
        }

        public void AnnounceButtonPressed(string message)
        {
            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var msg = SharedChatSystem.SanitizeAnnouncement(message, maxLength);
            SendMessage(new CluwneCommsConsoleAnnounceMessage(msg));
        }

        public void AlertLevelSelected(string message)
        {
        }

        public void BoomButtonPressed()//idk why this shit isnt working
        {
            SendMessage(new CluwneCommsConsoleBoomMessage());
        }

        public void AlertButtonPressed(string level, string message, string instructions)
        {
            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var msg = SharedChatSystem.SanitizeAnnouncement(message, maxLength);
            var instr = SharedChatSystem.SanitizeAnnouncement(instructions, maxLength);
            SendMessage(new CluwneCommsConsoleAlertMessage(level, msg, instr));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not CluwneCommsConsoleInterfaceState commsState)
                return;

            if (_menu != null)
            {
                _menu.CanAnnounce = commsState.CanAnnounce;
                _menu.AnnounceButton.Disabled = !_menu.CanAnnounce;

                _menu.CanAlert = commsState.CanAlert;
                _menu.AlertButton.Disabled = !_menu.CanAlert;

                _menu.UpdateAlertLevels(commsState.AlertLevels);
            }
        }
    }
}
