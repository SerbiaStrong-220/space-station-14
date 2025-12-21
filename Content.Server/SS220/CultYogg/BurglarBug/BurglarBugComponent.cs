// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;

namespace Content.Server.SS220.CultYogg.BurglarBug;

[RegisterComponent, Access(typeof(BurglarBugServerSystem))]
public sealed partial class BurglarBugComponent : Component
{
    [DataField]
    public float DamageRange = 3f;

    [DataField(required: true)]
    public float TimeToOpen;

    /// <summary>
    /// What type of emag effect this device will do
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EmagType EmagType = EmagType.Access;

    /// <summary>
    ///     Popup message shown when player stuck entity, but forgot to activate it.
    /// </summary>
    [DataField]
    public string? NotActivatedStickPopupCancellation;

    /// <summary>
    ///     Popup message shown when player stuck entity tryed on opened door.
    ///     If you want to check on stuck to opened door set this.
    ///     By default this logic is off.
    /// </summary>
    [DataField]
    public string? OpenedDoorStickPopupCancellation;

    [DataField]
    public bool Activated;

    [DataField]
    public bool IgnoreResistances;

    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public Entity<DoorComponent>? Door;

    [DataField]
    public TimeSpan? DoorOpenTime;
}
