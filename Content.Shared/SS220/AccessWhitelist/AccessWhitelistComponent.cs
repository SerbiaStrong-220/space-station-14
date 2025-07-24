// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;

namespace Content.Shared.SS220.AccessWhitelist;

/// <summary>
///     Similar to AccessReader but for components
/// </summary>

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class AccessWhitelistComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField(required: true), AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
