// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Stunnable;

namespace Content.Shared.SS220.MartialArts.Sequence.Effects;

public sealed partial class StunTargetCombatEffect : CombatSequenceEffect
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(5);

    public override void Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        var stun = Entity.System<SharedStunSystem>();

        stun.TryAddStunDuration(target, Time);
    }
}
