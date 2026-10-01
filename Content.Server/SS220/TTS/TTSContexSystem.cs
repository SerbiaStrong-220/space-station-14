// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.SS220.TTS;
using Content.Shared.VoiceMask;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.TTS;

public sealed partial class TTSContextSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public bool TryGetVoiceID(EntityUid uid, [NotNullWhen(true)] out ProtoId<TTSVoicePrototype>? voiceId)
    {
        voiceId = null;
        if (!TryComp(uid, out TTSComponent? senderComponent))
            return false;

        voiceId = senderComponent.VoicePrototypeId;

        if (TryGetVoiceMaskUid(uid, out var maskUid))
        {
            var voiceEv = new TransformSpeakerVoiceEvent(maskUid.Value, voiceId);
            RaiseLocalEvent(maskUid.Value, ref voiceEv);
            voiceId = voiceEv.VoiceId;
        }

        return voiceId is not null;
    }

    public bool TryGetVoiceMaskUid(EntityUid maskCarrier, [NotNullWhen(true)] out EntityUid? maskUid)
    {
        maskUid = null;

        if (_inventory.TryGetContainerSlotEnumerator(maskCarrier, out var carrierSlot, SlotFlags.MASK))
        {
            while (carrierSlot.NextItem(out var itemUid, out _))
            {
                if (HasComp<VoiceMaskComponent>(itemUid))
                {
                    maskUid = itemUid;
                    return true;
                }
            }
        }

        if (_container.TryGetContainer(maskCarrier, ImplanterComponent.ImplantSlotId, out var implantContainer))
        {
            foreach (var implant in implantContainer.ContainedEntities)
            {
                if (HasComp<VoiceMaskComponent>(implant))
                {
                    maskUid = implant;
                    return true;
                }
            }
        }

        return false;
    }
}

