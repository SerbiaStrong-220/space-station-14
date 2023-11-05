// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Bodycam;

[Serializable, NetSerializable]
public enum BodycamVisualsKey : byte
{
    Key,
    Layer
}

[Serializable, NetSerializable]
public enum BodycamVisuals : byte
{
    On,
    Off
}
