// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.Virology;

namespace Content.Server.SS220.Virology;

public sealed partial class VirologySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private BlindableSystem _blinding = default!;

    private void TickOverload()
    {
        var query = EntityQueryEnumerator<VirusHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
            Evaluate(uid, CountStrains(holder));
    }

    private int CountStrains(VirusHolderComponent holder)
    {
        var count = 0;
        foreach (var carried in holder.Viruses)
        {
            // we don't count suppressed strain for overload
            if (TryComp<VirusComponent>(carried, out var comp) && comp.SuppressedUntil == null)
                count++;
        }

        return count;
    }

    private void Evaluate(EntityUid host, int count)
    {
        if (count < 2)
        {
            Deactivate(host);
            return;
        }

        var overload = EnsureComp<VirusOverloadComponent>(host);
        overload.ReachedAt ??= _timing.CurTime;

        if (_timing.CurTime < overload.ReachedAt + overload.Delay)
            return;

        if (overload.ActivatedAt == null)
        {
            overload.ActivatedAt = _timing.CurTime;
            VirusChat.SendSelfMessage(_chatManager, EntityManager, host, Loc.GetString("virus-overload-weaken"), Color.Red);
        }

        var ramp = overload.RampDuration <= TimeSpan.Zero
            ? 1f
            : (float)Math.Clamp((_timing.CurTime - overload.ActivatedAt.Value) / overload.RampDuration, 0d, 1d);

        var tier = count == 2 ? 1 : 2;
        var tierSlow = tier == 1 ? overload.Slow2 : overload.Slow3;
        var tierBlur = tier == 1 ? overload.Blind2 : overload.Blind3;

        var slow = 1f - (1f - tierSlow) * ramp;
        if (Math.Abs(overload.Walk - slow) > 0.001f)
        {
            overload.Walk = slow;
            overload.Sprint = slow;
            Dirty(host, overload);
            _movement.RefreshMovementSpeedModifiers(host);
        }

        if (TryComp<BlindableComponent>(host, out var blindable))
        {
            var floor = (int)MathF.Round(tierBlur * ramp * blindable.MaxDamage);
            if (floor != overload.AppliedBlur)
            {
                overload.AppliedBlur = floor;
                _blinding.SetMinDamage((host, blindable), floor);
            }
        }
    }

    /// <summary>Deactivate overload</summary>
    public void Deactivate(EntityUid host)
    {
        if (!TryComp<VirusOverloadComponent>(host, out var overload))
            return;

        if (overload.AppliedBlur > 0 && TryComp<BlindableComponent>(host, out var blindable))
            _blinding.SetMinDamage((host, blindable), 0);

        RemComp<VirusOverloadComponent>(host);
    }
}
