using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.SS220.MimeRelic

[RegisterComponent]
public sealed partial class MimeRelicComponent : Component
{
    [DataField("wallPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WallPrototype = "WallInvisible";

    [DataField("invisibleWallActionEntity")] 
    public EntityUid? InvisibleWallActionEntity;        
}
