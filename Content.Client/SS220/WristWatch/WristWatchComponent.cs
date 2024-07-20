// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.WristWatch;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Client.SS220.WristWatch;

[RegisterComponent]
public sealed partial class WristWatchComponent : SharedWristWatchComponent
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<WristWatchStylePrototype>))]
    public string Style;
}
