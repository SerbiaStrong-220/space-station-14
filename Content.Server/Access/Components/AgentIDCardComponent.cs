using Robust.Shared.Audio;

namespace Content.Server.Access.Components;

/// <summary>
///     Allows an ID card to copy accesses from other IDs and to change the name, job title and job icon via an interface.
/// </summary>
[RegisterComponent]
public sealed partial class AgentIDCardComponent : Component
{
    // ss220 agentId tweak begin
    [DataField]
    public bool ShouldPopup = true;
    [DataField]
    public SoundSpecifier UseSound = new SoundCollectionSpecifier("sparks");
    // ss220 agentId tweak end
}
