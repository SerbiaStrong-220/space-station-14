using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.SS220.MimeRelic

[RegisterComponent]
public sealed partial class MimeRelicComponent : Component
{
    [DataField("wallPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WallPrototype = "WallInvisible";

    [DataField("cooldownTime")] 
    public TimeSpan CooldownTime = TimeSpan.FromMinutes(2); // still need to think of dynamic of gameplay, base mime have like 2 times more cooldown, than lifetime         

    [DataField("wallLifetime")] 
    public TimeSpan WallLifetime = TimeSpan.FromSeconds(30); // still need to think of dynamic of gameplay     

    // do i need to have here smthg like "lastUsedTime" or for fckng what it needed in component?

    [DataField("spawnedWallEntity")] 
    public EntityUid? SpawnedWallEntity;        
}
