using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Projectiles.Components;

/// <summary>
/// Acts as a tag for simplicity
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlockedProjectileComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public NetEntity? BlockerEntity = null;
}
