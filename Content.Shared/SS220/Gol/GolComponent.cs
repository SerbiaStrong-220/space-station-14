using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.Gol;
/// <summary>
///     Component required for entities to be able to do vocal emotions.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class GolComponent : Component
{
    [DataField("golAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [AutoNetworkedField]
    public string GolAction = "ActionGol";

    [DataField("golActionEntity")]
    [AutoNetworkedField]
    public EntityUid? GolActionEntity;

    /// <summary>
    /// Sound played when toggling the <see cref="SelectedMode"/> for this gun.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier GolSound = new SoundPathSpecifier("/Audio/SS220/Voice/Gol/gooool.ogg");
}
public sealed partial class GolActionEvent : InstantActionEvent
{
}
