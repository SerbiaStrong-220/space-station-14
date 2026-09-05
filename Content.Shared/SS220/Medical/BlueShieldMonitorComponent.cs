// © SS220, An EULA/CLA with a hosting restriction, full text: https://githubusercontent.com

using Content.Shared.Medical.CrewMonitoring;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Medical;

[RegisterComponent]
public sealed partial class BlueShieldMonitorComponent : Component
{
}

[Serializable, NetSerializable]
public enum BlueShieldMonitorUIKey : byte
{
    Key
}
