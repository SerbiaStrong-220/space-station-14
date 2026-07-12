using Content.Shared.Disposal.Components;
using Content.Shared.Interaction;
using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using ServerFelinidPipecrawlSystem = Content.Server.SS220.Felinid.FelinidPipecrawlSystem;

namespace Content.Server.Disposal.Tube;

// SS220-felinid-pipecrawl
public sealed partial class DisposalTubeSystem
{
    [Dependency] private readonly ServerFelinidPipecrawlSystem _felinidPipecrawlSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    private void InitializeFelinidPipecrawl()
    {
        SubscribeLocalEvent<DisposalTubeComponent, InteractUsingEvent>(OnFelinidPipecrawlInteractUsing);
        SubscribeLocalEvent<DisposalTubeComponent, FelinidPipeExtractionDoAfterEvent>(OnFelinidPipeExtractionFinished);
    }

    private void OnFelinidPipecrawlInteractUsing(Entity<DisposalTubeComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp<ToolComponent>(args.Used, out var tool) ||
            !TryFindActivePipecrawler(ent.Owner, out _, out var pipecrawl) ||
            !_toolSystem.HasQuality(args.Used, pipecrawl.ExtractionQuality, tool))
        {
            return;
        }

        args.Handled = _toolSystem.UseTool(
            args.Used,
            args.User,
            ent.Owner,
            pipecrawl.ExtractionDelay,
            tool.Qualities,
            new FelinidPipeExtractionDoAfterEvent(),
            toolComponent: tool);
    }

    private void OnFelinidPipeExtractionFinished(
        Entity<DisposalTubeComponent> ent,
        ref FelinidPipeExtractionDoAfterEvent args)
    {
        if (args.Cancelled ||
            !TryFindActivePipecrawler(ent.Owner, out var felinidUid, out _))
        {
            return;
        }

        _felinidPipecrawlSystem.TryForceExitPipecrawl(felinidUid, false);
    }

    private bool TryFindActivePipecrawler(
        EntityUid tubeUid,
        out EntityUid felinidUid,
        out FelinidPipecrawlComponent pipecrawl)
    {
        var query = EntityQueryEnumerator<FelinidPipecrawlComponent>();
        while (query.MoveNext(out var uid, out var candidate))
        {
            if (!candidate.Active ||
                candidate.CurrentTube != tubeUid && candidate.NextTube != tubeUid)
            {
                continue;
            }

            pipecrawl = candidate;
            felinidUid = uid;
            return true;
        }

        felinidUid = default;
        pipecrawl = default!;
        return false;
    }
}
