// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Damage.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StunsContactsComponent : Component
{
    public bool IsActive = true;

    /// <summary>
    /// For how much second we will stun entities which contacted us
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// For how much second we will stun entities which contacted us
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan StunDelayTime = TimeSpan.FromSeconds(1f);

    public Dictionary<EntityUid, TimeSpan> TimeEntitiesStunned = new();
}
