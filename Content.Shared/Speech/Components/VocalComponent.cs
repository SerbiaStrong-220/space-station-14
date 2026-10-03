using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;// SS220-scream-cooldown
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Speech.Components;

/// <summary>
///     Component required for entities to be able to do vocal emotions.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VocalComponent : Component
{
    //TODO: Wilhelm scream logic needs to be more generic
    /// <summary>
    /// Emote ID for screaming (for whilhelm scream)
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public ProtoId<EmotePrototype> ScreamId = "Scream";

    /// <summary>
    /// Sound specifier for Wilhelm scream
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier Wilhelm = new SoundPathSpecifier("/Audio/Voice/Human/wilhelm_scream.ogg");

    /// <summary>
    /// Odds that screaming will be a Wilhelm scream
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float WilhelmProbability = 0.0002f;

    /// <summary>
    /// Default Emote Action to grant
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EntProtoId? EmoteAction = "ActionScream";

    [DataField]
    [AutoNetworkedField]
    public EntityUid? EmoteActionEntity;

    // SS220-scream-cooldown-begin
    [DataField]
    public ScreamCooldownData ScreamCooldown = new();
    // SS220-scream-cooldown-end

    /// <summary>
    ///     Currently loaded emote sounds prototype, based on entity sex on humanoids.
    ///     Null if no valid prototype for entity sex was found.
    ///     Generally everything should have this set. This provides the sounds for Urists as well.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public ProtoId<EmoteSoundsPrototype>? EmoteSounds = null;

    // SS220 Chat-Special-Emote start
    //Special sounds for entity
    //Made it dictionaty so that user could load several packs of emotions from several items
    //Null if no valid prototype were loaded
    public Dictionary<EntityUid, EmoteSoundsPrototype>? SpecialEmoteSounds = null;
    // SS220 Chat-Special-Emote end
}

// SS220-scream-cooldown-begin
[DataDefinition, Serializable]
public sealed partial class ScreamCooldownData
{
    [DataField]
    public TimeSpan BaseCooldown = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan CooldownStep = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan ResetDelay = TimeSpan.FromMinutes(10);

    [ViewVariables(VVAccess.ReadWrite)]
    public int Count;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CooldownEnd;
}
// SS220-scream-cooldown-end
