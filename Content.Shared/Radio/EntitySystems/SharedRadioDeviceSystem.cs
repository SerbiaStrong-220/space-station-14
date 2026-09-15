using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.EntitySystems;

public abstract class SharedRadioDeviceSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    #region Toggling
    public void ToggleRadioMicrophone(EntityUid uid, EntityUid user, bool quiet = false, RadioMicrophoneComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetMicrophoneEnabled(uid, user, !component.Enabled, quiet, component);
    }

    public virtual void SetMicrophoneEnabled(EntityUid uid, EntityUid? user, bool enabled, bool quiet = false, RadioMicrophoneComponent? component = null) { }

    public void ToggleRadioSpeaker(EntityUid uid, EntityUid user, bool quiet = false, RadioSpeakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetSpeakerEnabled(uid, user, !component.Enabled, quiet, component);
    }

    public void SetSpeakerEnabled(EntityUid uid, EntityUid? user, bool enabled, bool quiet = false, RadioSpeakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Enabled = enabled;
        Dirty(uid, component);

        if (!quiet && user != null)
        {
            var state = Loc.GetString(component.Enabled ? "handheld-radio-component-on-state" : "handheld-radio-component-off-state");
            var message = Loc.GetString("handheld-radio-component-on-use", ("radioState", state));
            _popup.PopupEntity(message, user.Value, user.Value);
        }

        _appearance.SetData(uid, RadioDeviceVisuals.Speaker, component.Enabled);
        if (component.Enabled)
        {
            var activeRadio = EnsureComp<ActiveRadioComponent>(uid);
            activeRadio.Channels.UnionWith(component.Channels);

            // SS220-listen-only-radio-begin
            // Direct initialization of ListenOnlyChannels from encryption keys
            HashSet<ProtoId<RadioChannelPrototype>> listenOnly = new();
            if (TryComp<EncryptionKeyHolderComponent>(uid, out var keyHolder))
            {
                foreach (var keyUid in keyHolder.KeyContainer.ContainedEntities)
                {
                    if (TryComp<EncryptionKeyComponent>(keyUid, out var key))
                    {
                        listenOnly.UnionWith(key.ListenOnlyChannels);
                    }
                }
            }
            activeRadio.ListenOnlyChannels = listenOnly;
            Log.Info($"[SS220 Radio INIT] Initialized ListenOnlyChannels for {uid}. Count: {listenOnly.Count}");
            // SS220-listen-only-radio-end

            Dirty(uid, activeRadio);
        }
    #endregion
    }
}
