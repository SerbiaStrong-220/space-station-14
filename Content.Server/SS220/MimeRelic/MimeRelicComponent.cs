using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.SS220.MimeRelic
{
    [RegisterComponent]
    public sealed partial class MimeRelicComponent : Component
    {
        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string WallToPlacePrototype = "WallInvisible";

        [DataField]
        public TimeSpan CooldownTime = TimeSpan.FromSeconds(90); // still need to think of dynamic of gameplay.
        [DataField]
        public TimeSpan WallLifetime = TimeSpan.FromSeconds(20); // still need to think of dynamic of gameplay 
    }
}