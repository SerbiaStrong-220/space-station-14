// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Grab;

namespace Content.Shared.SS220.MartialArts.Sequence.Effects;

public sealed partial class GrabShoveKnockoutCombatEffect : CombatSequenceEffect
{
    [DataField(required: true)]
    public TimeSpan KnockdownTime;

    public override void Execute(Entity<MartialArtistComponent> user, EntityUid target)
    {
        var knockout = Entity.System<GrabShoveKnockoutSystem>();

        knockout.SetupKnockout(user, KnockdownTime);
    }
}
