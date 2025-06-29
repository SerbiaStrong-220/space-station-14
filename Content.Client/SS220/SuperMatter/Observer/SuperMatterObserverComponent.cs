// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SuperMatter.Ui;

namespace Content.Client.SS220.SuperMatter.Observer;

[RegisterComponent]
public sealed partial class SuperMatterObserverComponent : SharedSuperMatterObserverComponent
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<int, string> Names = new();
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<int, (bool Delaminates, TimeSpan ETOfDelamination)> DelaminationStatuses = new();
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<int, List<float>> Integrities = new();
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<int, List<float>> Pressures = new();
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<int, List<float>> Temperatures = new();
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<int, List<(float Value, float Derv)>> Matters = new();
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<int, List<(float Value, float Derv)>> InternalEnergy = new();




}
