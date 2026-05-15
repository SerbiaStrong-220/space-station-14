// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.AstralLeap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AstralLeapComponent : Component
{
    [DataField(required: true)]
    public EntProtoId AstralAction;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? AstralActionEntity;

    /// <summary>
    ///     Preparation time for entering the astral plane
    ///     Null if DoAfter shouldn't happen
    /// </summary>
    [DataField]
    public TimeSpan? AstralLeapDoAfterTime;

    /// <summary>
    ///     An entity that will be polymorphed for the astral
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> AstralEnt;
}
