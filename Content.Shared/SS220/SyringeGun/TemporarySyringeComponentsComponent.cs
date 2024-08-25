// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SyringeGun;

[RegisterComponent, NetworkedComponent]
public sealed partial class TemporarySyringeComponentsComponent : Component
{
    /// <summary>
    /// List of components to be removed
    /// </summary>
    public List<Type> Components = new();
}

