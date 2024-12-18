// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Botany;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.CultYogg.Fungus;

[RegisterComponent]
public sealed partial class FungusComponent : Component
{
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;
    [ViewVariables(VVAccess.ReadWrite), DataField("updateDelay")]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

    [DataField("cycleDelay")]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

    [DataField("lastCycle", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCycle = TimeSpan.Zero;

    [DataField("lastProduce")]
    public int LastProduce;

    [ViewVariables(VVAccess.ReadWrite), DataField("updateSpriteAfterUpdate")]
    public bool UpdateSpriteAfterUpdate;

    [ViewVariables(VVAccess.ReadWrite), DataField("age")]
    public int Age;

    [ViewVariables(VVAccess.ReadWrite), DataField("harvest")]
    public bool HarvestReady;

    [ViewVariables(VVAccess.ReadWrite), DataField("seed")]
    public SeedData? Seed;
}
