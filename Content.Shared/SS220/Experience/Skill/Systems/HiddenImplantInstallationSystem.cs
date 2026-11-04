// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Surgery.Components;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed partial class HiddenImplantInstallationSystem : SkillEntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<HiddenImplantInstallationComponent, GetSubdermalInstallLevel>(OnGetSubdermalInstallLevel);
    }

    private void OnGetSubdermalInstallLevel(Entity<HiddenImplantInstallationComponent> entity, ref GetSubdermalInstallLevel args)
    {
        if (_net.IsClient)
            return;

        var level = entity.Comp.InstallChances.Count == 0 ? HiddenInstallLevel.Easy : _random.Pick(entity.Comp.InstallChances);
        var hiddenInstalled = _random.Prob(entity.Comp.HiddenInstallChance);

        args.InstallLevel = level;
        args.Hidden = hiddenInstalled;
    }
}
