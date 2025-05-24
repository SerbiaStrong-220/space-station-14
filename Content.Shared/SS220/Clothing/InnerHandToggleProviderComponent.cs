// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InnerHandToggleProviderComponent : Component
{
    [ViewVariables]
    public Entity<InnerHandToggleableComponent>? InnerUser;

    [ViewVariables]
    public string? ContainerName;

    [ViewVariables]
    public string? HandName;
}
