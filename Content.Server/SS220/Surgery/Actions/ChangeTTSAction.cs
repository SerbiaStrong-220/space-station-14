// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Server.SS220.TTS;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.TTS;
using Robust.Shared.Player;

namespace Content.Server.SS220.Surgery.Action;

public sealed partial class ChangeTtsAction : ISurgeryGraphEdgeAction
{
    private static readonly LocId QuiTitle = "tts-change-surgery-window-title";
    private static readonly LocId QuiDescription = "tts-change-surgery-window-description";
    private static readonly LocId QuiPrompt = "tts-change-surgery-window-prompt";

    public void PerformAction(EntityUid uid, EntityUid userUid, EntityUid? used, IEntityManager entityManager)
    {
        var quiSystem = entityManager.System<QuickDialogSystem>();

        if (!entityManager.TryGetComponent<ActorComponent>(userUid, out var actorComp))
            return;

        quiSystem.OpenDialogForTtsVoicePrefereces(actorComp.PlayerSession,
            Loc.GetString(QuiTitle),
            Loc.GetString(QuiDescription),
            Loc.GetString(QuiPrompt),
            newPreferencesStr =>
            {
                if (!TtsVoicePreferences.TryParse(newPreferencesStr, out var pref))
                    return;

                var checkData = new TtsVoiceRequirementCheckData()
                {
                    Session = actorComp.PlayerSession,
                    Entity = uid
                };

                entityManager.System<TtsSystem>().SetVoicePreferences(uid, pref, checkData);
            },
            uid);
    }
}
