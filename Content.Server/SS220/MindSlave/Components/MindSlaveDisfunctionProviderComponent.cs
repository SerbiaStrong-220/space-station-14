// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;

namespace Content.Server.SS220.MindSlave.Components;

[RegisterComponent]
public sealed partial class MindSlaveDisfunctionProviderComponent : Component
{
    [DataField(required: true)]
    public DisfunctionParameters Disfunction = new();
}

[DataDefinition]
public sealed partial class DisfunctionParameters
{
    [DataField(required: true)]
    public Dictionary<MindSlaveDisfunctionType, List<string>> Disfunction = new();

    [DataField(required: true)]
    public DamageSpecifier DeadlyStageDamage = new();

    [DataField(required: true)]
    public string ProgressionPopup;
}
