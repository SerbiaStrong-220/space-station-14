// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt


using Content.Shared.Administration.Logs;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Experience.Skill;

public abstract partial class SkillEntitySystem : EntitySystem
{
    [Dependency] protected readonly ExperienceSystem Experience = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

}
