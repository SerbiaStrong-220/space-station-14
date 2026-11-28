// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Traits;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Body;

/// <summary>
/// On a brain: the lobby traits the character was born with that live in the brain.
/// They leave the body with the brain and come into the body it is put in, a lobotomy can cure them.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CongenitalTraitsComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<TraitPrototype>> Traits = new();
}
