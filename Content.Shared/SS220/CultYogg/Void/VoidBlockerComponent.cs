// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.CultYogg.Void;

/// <summary>
/// This is used to temporarily prevent from fast teleport with a VoidKey.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VoidBlockerComponent : Component;
