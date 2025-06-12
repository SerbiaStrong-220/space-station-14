// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.SS220.Undereducated;

namespace Content.Server.SS220.Undereducated;

[RegisterComponent]
public sealed partial class UndereducatedComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<LanguageReplacementsPrototype>), required: false)]
    public string Language = "";

    [DataField]
    public float ChanseToReplace = 0.05f;
}
