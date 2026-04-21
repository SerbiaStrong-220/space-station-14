namespace Content.Client.SS220.Vision;

/// <summary>
///     This component is used to store the entity of the light spawned for scotopic vision,
///     so it can be deleted when the component is removed or the player detaches from the entity.
/// </summary>
[RegisterComponent]
public sealed partial class ScotopicVisionVisualsComponent : Component
{
    [ViewVariables]
    public EntityUid? LightEntity;
}
