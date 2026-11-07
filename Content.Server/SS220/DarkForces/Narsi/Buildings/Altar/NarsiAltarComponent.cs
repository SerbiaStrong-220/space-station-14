using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar;

[RegisterComponent]
public sealed partial class NarsiAltarComponent : Component
{
    [DataField]
    public EntityUid? BuckledEntity;

    [DataField]
    public EntityUid? ActiveSound;

    [DataField]
    public DoAfterId? DoAfterId;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan RitualTimeoutTick;

    [DataField]
    public TimeSpan RitualsThreshold = TimeSpan.FromSeconds(120);

    [DataField]
    public NarsiRitualsProgressState State = NarsiRitualsProgressState.Idle;

    public NarsiRitualPrototype? ActualRitual { get; set; }

    [DataField(required: true)]
    public NarsiRitualVisualsParams VisualsParams = default!;
}

[DataDefinition]
public sealed partial class NarsiRitualVisualsParams
{
    [DataField]
    public TimeSpan VisualsThreshold = TimeSpan.FromSeconds(7);

    [DataField]
    public TimeSpan VisualsTick = TimeSpan.Zero;

    [DataField]
    public List<EntProtoId> VisualsEntities = new();
}
