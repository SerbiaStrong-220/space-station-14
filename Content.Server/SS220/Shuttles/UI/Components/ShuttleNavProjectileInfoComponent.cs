
namespace Content.Server.SS220.Shuttles.UI.Components;

[RegisterComponent]
public sealed partial class ShuttleNavProjectileInfoComponent : Component
{
    [DataField]
    public float Size = 0.5f;

    [DataField]
    public Color Color = Color.Yellow;
}
