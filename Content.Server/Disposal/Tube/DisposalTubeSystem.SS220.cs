using Content.Shared.Disposal.Components;
using Content.Shared.Interaction;
using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using ServerDisposalPipeCrawlerSystem = Content.Server.SS220.Felinid.DisposalPipeCrawlerSystem;

namespace Content.Server.Disposal.Tube;

// SS220-felinid-pipecrawl
public sealed partial class DisposalTubeSystem
{
    [Dependency] private ServerDisposalPipeCrawlerSystem _felinidPipecrawlSystem = default!;
    [Dependency] private SharedToolSystem _toolSystem = default!;

    private void InitializeDisposalPipeCrawler()
    {
        SubscribeLocalEvent<DisposalTubeComponent, InteractUsingEvent>(OnDisposalPipeCrawlerInteractUsing);
        SubscribeLocalEvent<DisposalTubeComponent, DisposalPipeExtractionDoAfterEvent>(OnPipeExtractionFinished);
    }

    private void OnDisposalPipeCrawlerInteractUsing(Entity<DisposalTubeComponent> ent, ref InteractUsingEvent args)
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
            new DisposalPipeExtractionDoAfterEvent(),
            toolComponent: tool);
    }

    private void OnPipeExtractionFinished(
        Entity<DisposalTubeComponent> ent,
        ref DisposalPipeExtractionDoAfterEvent args)
    {
        if (args.Cancelled ||
            !TryFindActivePipecrawler(ent.Owner, out var felinidUid, out _) ||
            felinidUid is not { } crawler)
        {
            return;
        }

        _felinidPipecrawlSystem.TryForceExitPipecrawl(crawler, false);
    }

    private bool TryFindActivePipecrawler(
        EntityUid tubeUid,
        out EntityUid? felinidUid,
        out DisposalPipeCrawlerComponent pipecrawl)
    {
        var query = EntityQueryEnumerator<DisposalPipeCrawlerComponent>();
        while (query.MoveNext(out var uid, out var candidate))
        {
            if (!candidate.InsidePipe ||
                candidate.CurrentTube != tubeUid && candidate.NextTube != tubeUid)
            {
                continue;
            }

            pipecrawl = candidate;
            felinidUid = uid;
            return true;
        }

        felinidUid = null;
        pipecrawl = default!;
        return false;
    }
}
