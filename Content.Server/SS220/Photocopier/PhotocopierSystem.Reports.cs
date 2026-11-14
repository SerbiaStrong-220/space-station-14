// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.SS220.Photocopier;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Photocopier;

public sealed partial class PhotocopierSystem
{
    /// <summary>
    /// Whether the copier can accept a report into its normal printing queue.
    /// </summary>
    /// <param name="uid">The copier receiving the report.</param>
    /// <param name="component">The copier component, if already resolved.</param>
    public bool CanPrintReport(EntityUid uid, PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (TerminatingOrDeleted(uid))
            return false;

        if (!component.Initialized)
            return false;

        if (!TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            return false;

        if (!this.IsPowered(uid, EntityManager, receiver))
            return false;

        if (component.State != PhotocopierState.Idle)
            return false;

        if (component.CopiesQueued > 0)
            return false;

        if (component.DocumentsToCopy.Count > 0)
            return false;

        if (!TryGetTonerCartridge(component, out var toner))
            return false;

        return toner.Charges > 0;
    }

    /// <summary>
    /// Queues one report for printing at the copier, with the normal delay, sound and toner consumption.
    /// </summary>
    /// <param name="uid">The copier receiving the report.</param>
    /// <param name="prototype">The paper prototype used for the printed report.</param>
    /// <param name="title">The printed paper's name.</param>
    /// <param name="content">The report content, with user text already escaped for paper formatting.</param>
    /// <param name="component">The copier component, if already resolved.</param>
    public bool TryQueueReport(EntityUid uid, EntProtoId prototype, string title, string content,
        PhotocopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanPrintReport(uid, component))
            return false;

        if (!_prototypeManager.TryIndex(prototype, out var reportPrototype))
            return false;

        if (!reportPrototype.TryGetComponent<PaperComponent>(out _, EntityManager.ComponentFactory))
            return false;

        Dictionary<Type, IPhotocopiedComponentData> data = [];
        data.Add(typeof(PaperComponent), new PaperPhotocopiedData { Content = content });
        var metadata = new PhotocopyableMetaData
        {
            PrototypeId = prototype.Id,
            EntityName = title,
        };
        QueueDocument(component, new PrintableDocumentData(data, metadata));
        if (!StartPrinting(uid, component, PhotocopierState.Printing, 1))
        {
            ResetState(uid, component);
            return false;
        }

        UpdateUserInterface(uid, component);
        TryUpdateVisualState(uid, component);
        return true;
    }
}
