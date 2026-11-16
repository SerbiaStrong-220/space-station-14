namespace Content.Server.SS220.LockPick.Components;

[RegisterComponent]
public sealed partial class TargetLockPickComponent : Component
{
    [DataField]
    public float ChanceToLockPick;

    public readonly float TimeToLockPick = 5f; //in seconds for DoAfter
}
