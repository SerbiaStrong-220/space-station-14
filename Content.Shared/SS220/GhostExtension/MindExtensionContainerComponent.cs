using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.GhostExtension;

[RegisterComponent]
public sealed partial class MindExtensionContainerComponent : Component
{
    [DataField]
    public EntityUid? MindExtension { get; set; }

    [MemberNotNullWhen(true, nameof(MindExtension))]
    public bool HasMindExtension => MindExtension is not null;
}
