using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Client.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.ManageTraits
{
    [UsedImplicitly]
    public sealed class ManageTraitsEui : BaseEui
    {
        private readonly ManageTraitsWindow _window;

        public ManageTraitsEui()
        {
            _window = new ManageTraitsWindow();
            _window.OnAddTrait += traitId => SendMessage(new AddTraitMessage(traitId));
            _window.OnRemoveTrait += traitId => SendMessage(new RemoveTraitMessage(traitId));
            _window.OnClose += () => SendMessage(new CloseEuiMessage());
        }

        public override void Opened() => _window.OpenCentered();
        public override void Closed() => _window.Close();

        public override void HandleState(EuiStateBase state)
        {
            if (state is ManageTraitsEuiState s)
                _window.UpdateState(s.TargetName, s.ActiveTraitIds, s.AllTraits);
        }
    }
}
