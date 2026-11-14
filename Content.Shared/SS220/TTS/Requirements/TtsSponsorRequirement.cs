using Content.Shared.Players;
using Content.Shared.SS220.Discord;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.SS220.TTS.Requirements;

[Serializable, NetSerializable]
public sealed partial class TtsSponsorRequirement : TtsVoiceRequirement
{
    [DataField(required: true)]
    public SponsorTier SponsorTier = SponsorTier.None;

    /// <summary>
    /// Is this exact tier required? Use this for stuff like developer rewards.
    /// </summary>
    [DataField]
    public bool IsExact = false;

    private static readonly Color SponsorTierMarkupColor = Color.Yellow;

    public override bool Check(IEntityManager entityManager, TtsVoiceRequirementCheckData data, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        if (SponsorTier == SponsorTier.None)
            return true;

        if (data.Session is not { } session)
            return true;

        var sponsorInfo = session.ContentData()?.SponsorInfo;
        if (!Inverted)
        {
            if (sponsorInfo != null && IsPassRequirement(sponsorInfo.Tiers))
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("tts-sponsor-requirement-whitelist-not-pass",
                ("isExact", IsExact),
                ("sponsorTier", GetSponsorTierMarkup())));
            return false;
        }
        else
        {
            if (sponsorInfo == null || !IsPassRequirement(sponsorInfo.Tiers))
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("tts-sponsor-requirement-blacklist-not-pass",
                ("isExact", IsExact),
                ("sponsorTier", GetSponsorTierMarkup())));
            return false;
        }

        string GetSponsorTierMarkup()
        {
            var msg = new FormattedMessage();

            msg.PushColor(SponsorTierMarkupColor);
            msg.AddText(SponsorTier.ToString());
            msg.Pop();

            return msg.ToMarkup();
        }
    }

    private bool IsPassRequirement(IEnumerable<SponsorTier> tiers)
    {
        return tiers.Any(t => IsExact ? t == SponsorTier : t >= SponsorTier);
    }
}
