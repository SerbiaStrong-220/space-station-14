// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.Bible;

[RegisterComponent]
/// <summary>
/// Adds ability to perform exorcism with this component on item. 
/// </summary>
public sealed partial class ExorcismPerformerComponent : Component
{
    /// <summary>
    /// Should exorcism be availiable only for creatures with BibleUserComponent.
    /// </summary>
    [DataField("requiresBibleUser")]
    public bool RequiresBibleUser = true;

    /// <summary>
    /// Prototype id of an exorcism action.
    /// </summary>
    [DataField("exorcismAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ExorcismAction = "ActionExorcism";
    /// <summary>
    /// Resolved exorcism action entity.
    /// </summary>
    [DataField("exorcismActionEntity")]
    public EntityUid? ExorcismActionEntity;

    /// <summary>
    /// Minimum length of a prayer message for exorcism to be available.
    /// </summary>
    [DataField("messageLengthMin")]
    public int MessageLengthMin = 60;
    /// <summary>
    /// Maximum length of a prayer message for exorcism to be available.
    /// </summary>
    [DataField("messageLengthMax")]
    public int MessageLengthMax = 120;

    /// <summary>
    /// Range in which exorcism effects will reach other entities.
    /// </summary>
    [DataField("range")]
    public float Range = 10f;

    /// <summary>
    /// Duration in seconds of visual light effects.
    /// </summary>
    [DataField("lightEffectDuration")]
    public float LightEffectDurationSeconds = 1f;

    /// <summary>
    /// Id of light behaviour to be started on exorcism performance.
    /// </summary>
    [DataField("lightBehaviourId")]
    public string LightBehaviourId = "exorcism_performance";
}
