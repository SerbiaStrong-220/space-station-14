// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;
using Content.Shared.StationRecords;
using JetBrains.Annotations;

namespace Content.Server.Delivery;

public sealed partial class DeliverySystem
{
    [PublicAPI]
    public void SetBaseSpesoReward(Entity<DeliveryComponent?> ent, int reward)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.BaseSpesoReward = Math.Max(0, reward);
        DirtyField(ent, nameof(DeliveryComponent.BaseSpesoReward));
    }

    [PublicAPI]
    public void SetRecipient(Entity<DeliveryComponent?> ent, uint? recordId)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (recordId is not { } id ||
            ent.Comp.RecipientStation is not { } station ||
            !_records.TryGetRecord<GeneralStationRecord>(new StationRecordKey(id, station), out var record))
        {
            return;
        }

        ent.Comp.RecipientName = record.Name;
        ent.Comp.RecipientJobTitle = record.JobTitle;
        _appearance.SetData(ent, DeliveryVisuals.JobIcon, record.JobIcon);

        if (TryComp<FingerprintReaderComponent>(ent.Owner, out var reader))
        {
            _fingerprintReader.SetAllowedFingerprints(
                (ent.Owner, reader),
                record.Fingerprint != null ? [record.Fingerprint] : []);
        }

        _label.Label(ent.Owner, ent.Comp.RecipientName);
        DirtyFields(ent, null, nameof(DeliveryComponent.RecipientName), nameof(DeliveryComponent.RecipientJobTitle));
    }
}

