// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Experience;

/// <summary>
/// Base component for adding sublevels which handles only add without any reset
/// </summary>
public abstract partial class BaseFreeSublevelPointsAddComponent : Component
{
    public int AddFreeSublevelPoints;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class AntagFreeSublevelPointsAddComponent : BaseFreeSublevelPointsAddComponent;
