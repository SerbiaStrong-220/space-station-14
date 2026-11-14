// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.SetSelector;

namespace Content.Client.SS220.SetSelector;

public sealed partial class SetSelectorSystem : SharedSetSelectorSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SetSelectorComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAfterHandleState(Entity<SetSelectorComponent> selector, ref AfterAutoHandleStateEvent args)
    {
        UpdateUI(selector);
    }

    protected override void UpdateUI(Entity<SetSelectorComponent> selector)
    {
        if (_ui.TryGetOpenUi(selector.Owner, SetSelectorUIKey.Key, out var bui))
            bui.Update();
    }
}
