// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using static Content.Shared.SS220.CluwneComms.CluwneCommsComponent;
using Content.Client.SS220.CluwneComms.UI;
using Content.Shared.Communications;

namespace Content.Client.SS220.CluwneComms.UI
{
    public sealed class CluwneCommsConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        [ViewVariables]
        private CluwneCommsConsoleMenu? _menu;

        [ViewVariables]
        public bool CanAnnounce { get; private set; }

        [ViewVariables]
        public bool CountdownStarted { get; private set; }

        [ViewVariables]
        private TimeSpan? _expectedCountdownTime;
        public int Countdown => _expectedCountdownTime == null ? 0 : Math.Max((int) _expectedCountdownTime.Value.Subtract(_gameTiming.CurTime).TotalSeconds, 0);

        public CluwneCommsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new CluwneCommsConsoleMenu(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            _menu?.Dispose();
        }
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not CluwneCommsConsoleInterfaceState commsState)
                return;

            CanAnnounce = commsState.CanAnnounce;

            if (_menu != null)
            {
                _menu.UpdateCountdown();
                _menu.AnnounceButton.Disabled = !CanAnnounce;
            }
        }

        public void AnnounceButtonPressed(string message)
        {
            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var msg = SharedChatSystem.SanitizeAnnouncement(message, maxLength);
            SendMessage(new CommunicationsConsoleAnnounceMessage(msg));
        }
    }
}
