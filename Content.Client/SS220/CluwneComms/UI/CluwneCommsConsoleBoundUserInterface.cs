// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Content.Shared.SS220.CluwneComms;
using Content.Shared.MassMedia.Systems;

namespace Content.Client.SS220.CluwneComms.UI
{
    public sealed class CluwneCommsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        [ViewVariables]
        private CluwneCommsConsoleMenu? _menu;

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<CluwneCommsConsoleMenu>();
            _menu.OnAnnounce += AnnounceButtonPressed;
            _menu.OnAlert += AlertButtonPressed;
            _menu.OnBoom += BoomButtonPressed;
        }

        public void AnnounceButtonPressed(string message)
        {
            var msg = SharedChatSystem.SanitizeAnnouncement(message, _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength));

            SendMessage(new CluwneCommsConsoleAnnounceMessage(msg));
        }

        public void BoomButtonPressed()
        {
            SendMessage(new CluwneCommsConsoleBoomMessage());
        }

        public void AlertButtonPressed(string level, string message, string instructions)
        {
            var msg = SharedChatSystem.SanitizeAnnouncement(message, _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength));
            var instr = SharedChatSystem.SanitizeAnnouncement(instructions, SharedNewsSystem.MaxContentLength);

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
                _menu.AlertLevelButton.Disabled = !_menu.CanAlert;

                _menu.UpdateAlertLevels(commsState.AlertLevels);

                _menu.AnnounceCountdownEnd = commsState.AnnouncementCooldownRemaining;
                _menu.AlertCountdownEnd = commsState.AlertCooldownRemaining;
            }
        }
    }
}
