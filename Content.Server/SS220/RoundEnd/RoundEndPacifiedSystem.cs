// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Explosion.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.SS220.RoundEnd;

namespace Content.Server.SS220.RoundEnd;

public sealed partial class RoundEndPacifiedSystem : SharedRoundEndPacifiedSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndPacifiedComponent, UseAttemptEvent>(OnUseAttempt);
    }

    private void OnUseAttempt(Entity<RoundEndPacifiedComponent> ent, ref UseAttemptEvent args)
    {
        if (!CheckInteraction(ent, args.Used) || HasComp<ProjectileGrenadeComponent>(args.Used))
            args.Cancel();
    }
}
