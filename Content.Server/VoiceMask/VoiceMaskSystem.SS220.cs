using Content.Shared.Inventory;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.VoiceMask;
using Content.Shared.VoiceMask;

namespace Content.Server.VoiceMask;

public partial class VoiceMaskSystem
{
    private void InitializeTTS()
    {
        SubscribeLocalEvent<VoiceMaskComponent, GetTtsVoiceOverrideEvent>(OnGetVoiceOverride);
        SubscribeLocalEvent<VoiceMaskComponent, InventoryRelayedEvent<GetTtsVoiceOverrideEvent>>(OnInventoryGetVoiceOverride);

        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeTtsVoicePreferencesMessage>(OnChangeVoice);
    }

    private void OnGetVoiceOverride(Entity<VoiceMaskComponent> entity, ref GetTtsVoiceOverrideEvent args)
    {
        args.Add(entity.Comp.VoicePreferences);
    }

    private void OnInventoryGetVoiceOverride(Entity<VoiceMaskComponent> entity, ref InventoryRelayedEvent<GetTtsVoiceOverrideEvent> args)
    {
        OnGetVoiceOverride(entity, ref args.Args);
    }

    private void OnChangeVoice(Entity<VoiceMaskComponent> ent, ref VoiceMaskChangeTtsVoicePreferencesMessage msg)
    {
        ent.Comp.VoicePreferences = msg.VoicePreferences;

        _popupSystem.PopupCursor(Loc.GetString("voice-mask-voice-popup-success"), msg.Actor);

        UpdateUI(ent);
    }
}
