// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.SS220.CultYogg.DeCultReminder
{
    [UsedImplicitly]
    public sealed class DeCultReminderEui : BaseEui
    {
        private readonly DeCultReminderWindow _window;

        public DeCultReminderEui()
        {
            _window = new();

            _window.AcceptButton.OnPressed += _ => _window.Close();
        }

        public override void Opened()
        {
            IoCManager.Resolve<IClyde>().RequestWindowAttention();
            _window.OpenCentered();
        }

        public override void Closed()
        {
            _window.Close();
        }
    }
}
