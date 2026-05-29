using Robust.Shared.GameStates;

namespace Content.Shared.SS220.QuadHearing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access([typeof(SharedQuadHearingSystem)], Other = AccessPermissions.Read)]
public sealed partial class QuadHearingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool ShowEffect = true;

    /// <summary>
    /// Minimum distance from the player at which new overlay targets can be registered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinDistance = 5f;

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;
}
