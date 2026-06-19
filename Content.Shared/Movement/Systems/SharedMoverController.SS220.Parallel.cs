// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Movement.Components;
using Robust.Shared.Threading;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    [Dependency] private IParallelManager _parallel = default!;

    private readonly struct MobMovementJob : IParallelBulkRobustJob
    {
        public int BatchSize { get; init; }

        public SharedMoverController Controller { get; init; }
        public IReadOnlyList<Entity<InputMoverComponent>> Entities { get; init; }
        public float FrameTime { get; init; }

        public void ExecuteRange(int startIndex, int endIndex)
        {
            HashSet<EntityUid> colliderHashset = new();
            for (var i = startIndex; i < endIndex; i++)
            {
                Controller.HandleMobMovement(Entities[i], FrameTime, true, ref colliderHashset);
            }
        }
    }

    protected void ProcessMobMovementParallel(IReadOnlyList<Entity<InputMoverComponent>> movers, float frameTime, int threadCount)
    {
        var job = new MobMovementJob
        {
            Controller = this,
            Entities = movers,
            FrameTime = frameTime,
            BatchSize = (int)(movers.Count / threadCount) + 1
        };

        _parallel.ProcessNow(job, movers.Count);
    }
}
