// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Cloning;
using Content.Shared.Cloning.Events;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Server.SS220.LimitationRevive;

/// <summary>
/// This handles limiting the number of defibrillator resurrections
/// </summary>
public sealed class DyingBrainSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<LimitationReviveComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<LimitationReviveComponent, CloningEvent>(OnCloning);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DyingBrainComponent>();

        while (query.MoveNext(out var uid, out var limitationRevive))
        {

        }
    }
}
