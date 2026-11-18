// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Server.SS220.TTS;
using Content.Shared.Interaction;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.TTS.Requirements;
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
            newPreferencesStr => OnVoiceChosen(uid, userUid, actorComp.PlayerSession, newPreferencesStr, entityManager),
            uid);
    }

    // The answer may come long after the step: the surgeon must still be next to the patient.
    private static void OnVoiceChosen(EntityUid patient, EntityUid surgeon, ICommonSession session, string newPreferencesStr, IEntityManager entityManager)
    {
        if (!entityManager.EntityExists(patient) || !entityManager.EntityExists(surgeon))
            return;

        if (!entityManager.System<SharedInteractionSystem>().InRangeUnobstructed(surgeon, patient))
            return;

        if (!TtsVoicePreferences.TryParse(newPreferencesStr, out var chosen))
            return;

        var tts = entityManager.System<TtsSystem>();
        var checkData = new TtsVoiceRequirementCheckData()
        {
            Session = session,
            Entity = patient
        };

        var available = tts.RemoveNotAvailableVoices(chosen, checkData);
        if (available.Count == 0)
            return;

        // providers whose chosen voice was rejected keep the patient's current voice instead of losing it
        if (tts.TryGetVoicePreferences(patient, out var current, allowOverride: false))
        {
            current.HardMergeWith(available, withIndexes: true);
            available = current;
        }

        tts.SetVoicePreferences(patient, available);
    }
}
