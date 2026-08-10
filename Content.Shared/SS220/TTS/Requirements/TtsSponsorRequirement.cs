using Content.Shared.Players;
using Content.Shared.SS220.Discord;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.TTS.Requirements;

[Serializable, NetSerializable]
public sealed partial class TtsSponsorRequirement : TtsVoiceRequirement
{
    [DataField(required: true)]
    public SponsorTier SponsorTier = SponsorTier.None;

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
            if (sponsorInfo != null && sponsorInfo.Tiers.Contains(SponsorTier))
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("tts-sponsor-requirement-whitelist-not-pass", ("sponsorTier", SponsorTier.ToString())));
            return false;
        }
        else
        {
            if (sponsorInfo == null || !sponsorInfo.Tiers.Contains(SponsorTier))
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("tts-sponsor-requirement-blacklist-not-pass", ("sponsorTier", SponsorTier.ToString())));
            return false;
        }
    }
}
