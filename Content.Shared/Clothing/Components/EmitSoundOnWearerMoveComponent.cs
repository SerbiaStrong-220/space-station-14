using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Sunrise.Clothing.Components;

/// <summary>
///  Указывает, что предмет одежды издает звук при движении.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmitSoundOnWearerMoveComponent : Component
{
    /// <summary>
    /// Звук, который будет проигрываться.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier Sound = default!;

    /// <summary>
    /// Требуется ли гравитация для работы предмета.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresGravity = true;

    /// <summary>
    /// Координаты, где был проигран прошлый звук.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates LastPosition = EntityCoordinates.Invalid;

    /// <summary>
    ///   Расстояние, пройденное с момента воспроизведения звука.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float SoundDistance = 0f;

    /// <summary>
    ///   Надет ли этот предмет в корректный слот инвентаря.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool IsValidSlot = true;
}