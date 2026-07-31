// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Pointing;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SS220.AltBlocking;
using Content.Shared.SS220.Mind;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.SiliconComponents;

public sealed partial class SiliconModuleSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private static readonly string PartContainerPrefix = "silicon_component";

    public override void Initialize()
    {
        base.Initialize();
    }


}

[ByRefEvent]
public record struct SiliconModuleGotInserted(EntityUid Owner)
{
}

[ByRefEvent]
public record struct SiliconModuleGotRemoved(EntityUid Owner)
{
}

[ByRefEvent]
public record struct SiliconModuleInserted(EntityUid Module)
{
}

[ByRefEvent]
public record struct SiliconModuleRemoved(EntityUid Module)
{
}


