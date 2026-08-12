using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.VoiceMask;
using Content.Shared.VoiceMask;

namespace Content.Server.VoiceMask;

public partial class VoiceMaskSystem
{
    private void InitializeSS220()
    {
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeTtsVoicePreferencesMessage>(OnChangeVoice);
        SubscribeLocalEvent<VoiceMaskComponent, GetTtsVoiceOverrideEvent>(OnGetVoiceOverride);
        SubscribeLocalEvent<VoiceMaskComponent, InventoryRelayedEvent<GetTtsVoiceOverrideEvent>>(OnInventoryGetVoiceOverride);
        SubscribeLocalEvent<VoiceMaskComponent, AfterInteractEvent>(OnInteract);
    }

    private void OnChangeVoice(Entity<VoiceMaskComponent> ent, ref VoiceMaskChangeTtsVoicePreferencesMessage msg)
    {
        ent.Comp.VoicePreferences = msg.VoicePreferences;
        _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-voice-changed"), ent, msg.Actor);

        UpdateUI(ent);
    }

    private void OnGetVoiceOverride(Entity<VoiceMaskComponent> entity, ref GetTtsVoiceOverrideEvent args)
    {
        args.Add(entity.Comp.VoicePreferences);
    }

    private void OnInventoryGetVoiceOverride(Entity<VoiceMaskComponent> entity, ref InventoryRelayedEvent<GetTtsVoiceOverrideEvent> args)
    {
        OnGetVoiceOverride(entity, ref args.Args);
    }

    private void OnInteract(Entity<VoiceMaskComponent> ent, ref AfterInteractEvent args)
    {
        if (!TryComp<TtsComponent>(args.Target, out var targetTts))
            return;

        ent.Comp.VoicePreferences = targetTts.VoicePreferences.Clone();
        _popupSystem.PopupCursor(Loc.GetString("voice-mask-popup-voice-copied"), args.User);

        UpdateUI(ent);
    }
}
