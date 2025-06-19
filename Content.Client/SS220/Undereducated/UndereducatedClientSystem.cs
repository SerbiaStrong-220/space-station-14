// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Undereducated;
using Robust.Shared.Player;

namespace Content.Client.SS220.Undereducated;

public sealed class UndereducatedClientSystem : EntitySystem
{
    private UndereducatedWindow? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UndereducatedComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(Entity<UndereducatedComponent> ent, ref PlayerAttachedEvent args)
    {
        if (!ent.Comp.Tuned)
        {
            _window?.Close();

            _window = new UndereducatedWindow(ent.Comp);
            _window.OnClose += () =>
            {
                if (!TryComp<UndereducatedComponent>(ent, out var comp) || comp.Tuned)
                    _window = null;
                else
                {
                    var ev = new UndereducatedConfigRequestEvent(GetNetEntity(ent), _window.SelectedLanguage, _window.SelectedChance);
                    RaiseNetworkEvent(ev);
                    _window = null;
                }
            };
            _window.OpenCentered();
        }
    }
}
