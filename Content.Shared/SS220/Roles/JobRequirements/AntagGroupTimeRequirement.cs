// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.Roles.JobRequirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class AntagGroupTimeRequirement : JobRequirement
{
    /// <summary>
    /// Which antag gtoup needs the required amount of time.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AntagGroupPrototype> AntagGroup = default!;

    /// <summary>
    /// How long (in seconds) this requirement is.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Time;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();
        var playTime = TimeSpan.Zero;

        //Check antag group
        var antagGoup = protoManager.Index(AntagGroup);
        var antagsList = antagGoup.Roles;
        string? proto;

        //Check all antags playTime in group
        foreach (var antag in antagsList)
        {
            proto = protoManager.Index(antag).PlayTimeTracker;

            if (proto is null)
                continue;

            playTimes.TryGetValue(proto, out var antagPlayTime);
            playTime += antagPlayTime;
        }

        var antagGroupDiff = Time.TotalMinutes - playTime.TotalMinutes;
        var nameAntagGroup = antagGoup.Name;

        if (!Inverted)
        {
            if (antagGroupDiff <= 0)
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-antag-group-insufficient",
                ("time", Math.Ceiling(antagGroupDiff)),
                ("antagGroup", Loc.GetString(nameAntagGroup)),
                ("antagGroupColor", antagGoup.Color.ToHex())));
            return false;
        }

        if (antagGroupDiff <= 0)
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-antag-group-too-high",
                ("time", -antagGroupDiff),
                ("antagGroup", Loc.GetString(nameAntagGroup)),
                ("antagGroupColor", antagGoup.Color.ToHex())));
            return false;
        }

        return true;
    }
}
