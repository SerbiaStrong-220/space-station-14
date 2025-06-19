// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Undereducated;
using Robust.Client.UserInterface;
using Robust.Shared.Player;

namespace Content.Client.SS220.Undereducated;

public sealed class UndereducatedClientSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        if (TryComp<UndereducatedComponent>(args.Entity, out var comp) && !comp.Tuned)
        {
            var uiController = _uiManager.GetUIController<UndereducatedUiController>();
            uiController?.Open(args.Entity, comp);
        }
    }
}
