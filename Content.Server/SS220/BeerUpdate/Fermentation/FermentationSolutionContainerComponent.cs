namespace Content.Server.SS220.BeerUpdate.Fermentation;

public sealed partial class FermentationSolutionContainerComponent : Component
{
    [DataField]
    public string SolutionName = "";

    [DataField]
    public bool Active = false;
}
