// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction.Events;
using Content.Shared.SS220.RoundEnd;

public sealed partial class RoundEndPacifiedSystem : SharedRoundEndPacifiedSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndPacifiedComponent, UseAttemptEvent>(OnUseAttempt);
    }

    private void OnUseAttempt(Entity<RoundEndPacifiedComponent> ent, ref UseAttemptEvent args)
    {
        bool allowed = CheckInteraction(ent, args.Used);
        if (!allowed)
        {
            args.Cancel();
        }
    }
}
