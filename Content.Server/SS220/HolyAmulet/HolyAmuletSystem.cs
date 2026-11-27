// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.HolyAmulet;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Server.SS220.HolyAmulet;

public sealed partial class HolyAmuletSystem : SharedHolyAmuletSystem
{
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyAmuletComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<HolyAmuletComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        comp.NextUpdate = _timing.CurTime + comp.UpdateCooldown;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<HolyAmuletComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextUpdate > _timing.CurTime)
                continue;

            comp.NextUpdate += comp.UpdateCooldown;

            SetActive((uid, comp), HasUnholyNearby((uid, comp)));
        }
    }

    private void SetActive(Entity<HolyAmuletComponent> ent, bool active)
    {
        var comp = ent.Comp;

        if (active == comp.Active)
            return;

        comp.Active = active;
        DirtyField(ent, comp, nameof(HolyAmuletComponent.Active));

        _light.SetEnabled(ent, active);
    }

    private bool HasUnholyNearby(Entity<HolyAmuletComponent> ent)
    {
        var comp = ent.Comp;
        var amuletXform = Transform(ent);

        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var mobXform))
        {
            if (!_transform.InRange((ent.Owner, amuletXform), (uid, mobXform), comp.Range))
                continue;

            if (_whitelist.IsWhitelistPass(comp.DetectWhitelist, uid))
                return true;
        }

        return false;
    }
}

