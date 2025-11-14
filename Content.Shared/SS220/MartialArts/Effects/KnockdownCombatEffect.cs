// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Stunnable;

namespace Content.Shared.SS220.MartialArts.Effects;

public sealed partial class KnockdownCombatEffect : CombatSequenceEffect
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(5);

    [DataField]
    public bool Refresh = true;

    [DataField]
    public bool Drop = true;

    [DataField]
    public bool Force = false;

    public override void Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        var stun = Entity.System<SharedStunSystem>();

        stun.TryKnockdown((target, null), time: Time, refresh: Refresh, drop: Drop, force: Force);
    }
}
