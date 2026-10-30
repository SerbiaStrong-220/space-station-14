// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared.SS220.MartialArts.Sequence.Conditions;

public sealed partial class EyeProtectionCombatCondition : CombatSequenceCondition
{
    [DataField("gt")]
    public TimeSpan? GreaterThan;

    [DataField("lt")]
    public TimeSpan? LessThan;

    [DataField("eq")]
    public TimeSpan? EqualsTo;

    public override bool Execute(Entity<MartialArtistComponent> user, EntityUid target)
    {
        var ev = new GetEyeProtectionEvent();
        Entity.EventBus.RaiseLocalEvent(user, ev);

        var greaterCondition = true;
        var lessCondition = true;
        var equalsCondition = true;

        if (GreaterThan != null && ev.Protection < GreaterThan)
            greaterCondition = false;

        if (LessThan != null && ev.Protection > LessThan)
            lessCondition = false;

        // accuracy of seconds
        if (EqualsTo != null && TimeSpan.FromSeconds(Math.Ceiling(ev.Protection.TotalSeconds)) != EqualsTo)
            equalsCondition = false;

        return greaterCondition && lessCondition && equalsCondition;
    }
}
