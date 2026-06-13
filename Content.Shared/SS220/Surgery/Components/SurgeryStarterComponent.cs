// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Surgery.Components;

/// <summary>
/// This component used to define items which can start operations
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryStarterComponent : Component;
