// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology;

public readonly record struct PathologyEffectArgs(
    EntityUid Target,
    PathologyInstanceData Data,
    IEntityManager EntityManager,
    TimeSpan CurTime,
    bool IsClient);
