// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;
using Content.Shared.StationRecords;
using JetBrains.Annotations;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

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
            return;

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

    [PublicAPI]
    public bool ReplaceContents(Entity<DeliveryComponent?> ent, string protoId)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.Container, out var container))
            return false;

        if (!_protoMan.TryIndex<EntityPrototype>(protoId, out var proto) || proto.Abstract)
        {
            Log.Warning($"Can't replace the contents of {ToPrettyString(ent.Owner)} with '{protoId}': no such prototype.");
            return false;
        }

        var spawn = Spawn(proto.ID, Transform(ent.Owner).Coordinates);
        if (!TryComp(spawn, out TransformComponent? xform) || !TryComp(spawn, out MetaDataComponent? meta))
        {
            PredictedDel(spawn);
            Log.Error($"Can't put '{protoId}' into {ToPrettyString(ent.Owner)}: it has no transform or metadata.");
            return false;
        }

        var previous = container.ContainedEntities.ToArray();
        if (!_container.Insert(
                new Entity<TransformComponent?, MetaDataComponent?, PhysicsComponent?>(spawn, xform, meta, null),
                container,
                Transform(ent.Owner),
                force: true))
        {
            PredictedDel(spawn);
            Log.Error($"Can't put '{protoId}' into {ToPrettyString(ent.Owner)}: the container refused it.");
            return false;
        }

        foreach (var uid in previous)
        {
            if (Deleted(uid))
                continue;

            _container.Remove(uid, container, reparent: false, force: true);
            PredictedDel(uid);
        }

        return true;
    }

    [PublicAPI]
    public bool ClearContents(Entity<DeliveryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.Container, out var container))
            return false;

        _container.CleanContainer(container);
        return true;
    }
}

