// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.HolyAmulet;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.SS220.HolyAmulet;

public sealed partial class HolyAmuletSystem : SharedHolyAmuletSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyAmuletComponent, ComponentShutdown>(OnShutdown);
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<HolyAmuletComponent, SpriteComponent>();
        var time = (float)_timing.CurTime.TotalSeconds;

        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            if (!comp.Active)
            {
                if (sprite.Rotation != Angle.Zero)
                    _sprite.SetRotation((uid, sprite), Angle.Zero);

                continue;
            }

            var phase = time * comp.ShakeSpeed + uid.GetHashCode() * 0.1f;
            _sprite.SetRotation((uid, sprite), comp.ShakeAngle * MathF.Sin(phase));
        }
    }

    private void OnShutdown(Entity<HolyAmuletComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetRotation((ent, sprite), Angle.Zero);
    }
}

