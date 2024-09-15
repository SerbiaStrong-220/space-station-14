using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState] //SS220 Add stand still time
public sealed partial class DamagedByContactComponent : Component
{
    [DataField("nextSecond", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextSecond = TimeSpan.Zero;

    [ViewVariables]
    public DamageSpecifier? Damage;

    //SS220 Add stand still time begin
    [ViewVariables, AutoNetworkedField]
    public TimeSpan LastMovement = TimeSpan.Zero;

    [DataField, ViewVariables, AutoNetworkedField]
    public TimeSpan StandStillTime = TimeSpan.Zero;
    //SS220 Add stand still time end
}
