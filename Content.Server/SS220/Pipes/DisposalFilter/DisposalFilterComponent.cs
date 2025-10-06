using Content.Shared.SS220.Pipes.DisposalFilter;

namespace Content.Server.SS220.Pipes.DisposalFilter;

[RegisterComponent]
public sealed partial class DisposalFilterComponent : Component
{
    [DataField("filter")]
    public List<FilterRule> FilterByDir = new();

    /// <summary>
    /// Which <see cref="Direction"/> use, if none of the filters match the conditions/>
    /// </summary>
    [DataField]
    public Direction? BaseDirection;
}

