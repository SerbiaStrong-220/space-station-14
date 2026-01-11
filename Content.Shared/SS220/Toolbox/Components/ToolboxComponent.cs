using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Toolbox.Components;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class ToolboxComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
