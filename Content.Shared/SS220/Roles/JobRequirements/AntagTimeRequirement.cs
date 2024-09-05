// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Serilog;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.Roles.JobRequirements;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class AntagTimeRequirement : JobRequirement
{
    /// <summary>
    /// What particular antag role they need the time requirement with.
    /// </summary>
    [DataField("role", required: true)]
    public ProtoId<AntagPrototype> Antag = default!;

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

        var antag = protoManager.Index(Antag);
        var proto = antag.PlayTimeTracker;

        //Doesn't block something if there is no PlayTimeTracker field in the AntagPrototype
        if (proto is null)
            return true;

        playTimes.TryGetValue(proto, out var antagTime);
        var antagDiff = Time.TotalMinutes - antagTime.TotalMinutes;

        if (!Inverted)
        {
            if (antagDiff <= 0)
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-antag-insufficient",
                ("time", Math.Ceiling(antagDiff)),
                ("antagName", Loc.GetString(proto)),
                ("antagColor", antag.AntagColor.ToHex())));
            return false;
        }

        if (antagDiff <= 0)
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-antag-too-high",
                ("time", -antagDiff),
                ("antagName", Loc.GetString(proto)),
                ("antagColor", antag.AntagColor.ToHex())));
            return false;
        }

        return true;
    }
}
