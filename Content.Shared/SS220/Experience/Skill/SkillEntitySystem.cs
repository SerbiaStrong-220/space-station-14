// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt


using Content.Shared.Administration.Logs;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Experience.Skill;

public abstract partial class SkillEntitySystem
{
    [Dependency] protected ExperienceSystem Experience = default!;
    [Dependency] protected IGameTiming GameTiming = default!;

    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedContainerSystem _container = default!;

}
