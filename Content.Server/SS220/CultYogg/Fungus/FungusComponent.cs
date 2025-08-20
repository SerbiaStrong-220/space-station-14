// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Botany;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.CultYogg.Fungus;

[RegisterComponent]
public sealed partial class FungusComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCycle = TimeSpan.Zero;

    [DataField]
    public int LastProduce;

    [DataField]
    public bool UpdateSpriteAfterUpdate;

    [DataField]
    public int Age;

    [DataField]
    public bool HarvestReady;

    [DataField]
    public SeedData? Seed;
}
