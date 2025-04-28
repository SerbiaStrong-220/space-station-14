using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Loadouts.SpeciesLoadout;

[Prototype("speciesLoadout")]
public sealed partial class SpeciesLoadoutPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public string Species = string.Empty;

    [DataField]
    public List<string> BlacklistJobs = new();

    [DataField]
    public Dictionary<string, List<string>> Storage { get; set; } = new();
}
