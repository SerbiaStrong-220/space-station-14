// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction.Events;

namespace Content.Shared.SS220.NonTactile;

public sealed partial class NonTactileSystem : EntitySystem
{
    private readonly LocId _failureString = "hugging-failure-generic-non-tactile";
    private readonly LocId _othersMessage = "hugging-failure-generic-non-tactile-others";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NonTactileComponent, InteractionSuccessEvent>(OnInteractionSuccess);
        SubscribeLocalEvent<NonTactileComponent, InteractionFailureEvent>(OnInteractionFailure);
    }

    private void OnInteractionSuccess(Entity<NonTactileComponent> ent, ref InteractionSuccessEvent args)
    {
        args.Message = _failureString;
        args.Sound = null;
        args.MessagePerceivedByOthers = _othersMessage;
    }

    private void OnInteractionFailure(Entity<NonTactileComponent> ent, ref InteractionFailureEvent args)
    {
        args.Message = _failureString;
        args.Sound = null;
        args.MessagePerceivedByOthers = _othersMessage;
    }
}

