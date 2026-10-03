using Content.Shared.Chat;
using Robust.Shared.GameStates;

namespace Content.Shared.Emoting;

/// <summary>
/// Enables emoting of an entity.
/// <seealso cref="EmoteAttemptEvent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmotingComponent : Component
{
    /// <summary>
    /// Emotes attempts are cancelled if not true.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(typeof(EmoteSystem), Friend = AccessPermissions.ReadWrite, Other = AccessPermissions.Read)]
    public bool Enabled = true;

    // SS220 Chat-Emote-Cooldown begin
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ChatEmoteCooldown = TimeSpan.FromSeconds(0.5);

    [ViewVariables]
    [Access(typeof(SharedChatSystem), Friend = AccessPermissions.ReadWrite, Other = AccessPermissions.Read)]
    public TimeSpan? LastChatEmoteTime;
    // SS220 Chat-Emote-Cooldown end
}
