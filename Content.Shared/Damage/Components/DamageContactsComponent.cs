using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[NetworkedComponent, RegisterComponent]
public sealed partial class DamageContactsComponent : Component
{
    /// <summary>
    /// The damage done each second to those touching this entity
    /// </summary>
    [DataField("damage", required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Entities that aren't damaged by this entity
    /// </summary>
    [DataField("ignoreWhitelist")]
    public EntityWhitelist? IgnoreWhitelist;

    //SS220 Add ignore blacklist begin
    /// <summary>
    /// Entities that damaged by this entity
    /// </summary>
    [DataField]
    public EntityWhitelist? IgnoreBlacklist;
    //SS220 Add ignore blacklist end

    //SS220 Add stand still time begin
    /// <summary>
    /// How many seconds does a entity need to stand still to start taking damage
    /// </summary>
    [DataField]
    public TimeSpan StandStillTime = TimeSpan.Zero;
    //SS220 Add stand still time end
}
