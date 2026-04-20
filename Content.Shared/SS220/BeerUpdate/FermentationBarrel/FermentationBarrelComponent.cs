namespace Content.Shared.SS220.BeerUpdate.FermentationBarrel;

[RegisterComponent]
public sealed partial class FermentationBarrelComponent : Component
{
    [DataField("solution")]
    public string SolutionName = SharedFermentationBarrel.SolutionName;

    [DataField]
    public float ElapsedTime = 0f;

    [DataField]
    public bool IsActive = false;

    [DataField]
    public bool IsDrawMode = false;

    [DataField]
    public Dictionary<string, bool> ReactionsFired = new();
}

[RegisterComponent]
public sealed partial class ActiveFermentationBarrelComponent : Component { }
