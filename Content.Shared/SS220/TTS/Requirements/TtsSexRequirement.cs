using Content.Shared.Humanoid;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.SS220.TTS.Requirements;

[Serializable, NetSerializable]
public sealed partial class TtsSexRequirement : TtsVoiceRequirement
{
    [DataField(required: true)]
    public List<Sex> Sexes = [];

    private static readonly Color SexMarkupColor = Color.Yellow;

    public override bool Check(IEntityManager entityManager, TtsVoiceRequirementCheckData data, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        if (Sexes.Count == 0)
            return true;

        if (data.Profile is { } profile && !CheckSex(profile.Sex, out reason))
            return false;

        if (data.Entity is { } entity && entityManager.TryGetComponent<HumanoidProfileComponent>(entity, out var profileComp) && !CheckSex(profileComp.Sex, out reason))
            return false;

        return true;
    }

    private bool CheckSex(Sex sex, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        if (!Inverted)
        {
            if (!Sexes.Contains(sex))
            {
                var sexesList = EnumerateSexesForMessage().ToList();

                reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("tts-sex-requirement-whitelist-not-pass",
                    ("sexesCount", sexesList.Count),
                    ("sexesList", string.Join(", ", sexesList))));
                return false;
            }
        }
        else
        {
            if (Sexes.Contains(sex))
            {
                var sexesList = EnumerateSexesForMessage().ToList();

                reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("tts-sex-requirement-blacklist-not-pass",
                    ("sexesCount", sexesList.Count),
                    ("sexesList", string.Join(", ", sexesList))));
                return false;
            }
        }

        reason = null;
        return true;

        IEnumerable<string> EnumerateSexesForMessage()
        {
            foreach (var sex in Sexes)
            {
                var msg = new FormattedMessage();

                msg.PushColor(SexMarkupColor);
                msg.AddText(sex.ToString());
                msg.Pop();

                yield return msg.ToMarkup();
            }
        }
    }
}
