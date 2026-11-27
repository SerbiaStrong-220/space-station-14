using Content.Shared.Chat;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Body;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChatHearingRangeMultiplierComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<InGameICChatType, float> Multipliers = [];
}
