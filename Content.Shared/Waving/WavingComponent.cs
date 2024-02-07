using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Waving;

/// <summary>
/// An emoting wag for markings.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WavingComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionToggleWaving";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public ProtoId<EmotePrototype> EmoteId = "WavTail";

    /// <summary>
    /// Suffix to add to get the animated marking.
    /// </summary>
    public string Suffix = "Animated";

    /// <summary>
    /// Is the entity currently wagging.
    /// </summary>
    [DataField]
    public bool Waving = false;
}
