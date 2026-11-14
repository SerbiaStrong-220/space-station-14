namespace Content.Server.SS220.DarkForces.Saint.Items.Cross;

[RegisterComponent]
public sealed partial class SaintCrossComponent : Component
{
    [DataField]
    public bool Sainted = false;

    [DataField]
    public TimeSpan NextTickToUpdate;
}