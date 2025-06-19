// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Gameplay;
using Content.Shared.SS220.Undereducated;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.SS220.Undereducated
{
    public sealed class UndereducatedUiController : UIController
    {
        private UndereducatedWindow? _window;

        public void Open(EntityUid entity, UndereducatedComponent comp)
        {
            _window?.Close();

            _window = new UndereducatedWindow(entity, comp);
            _window.OnClose += () => _window = null;
            _window.OpenCentered();
        }
    }
}
