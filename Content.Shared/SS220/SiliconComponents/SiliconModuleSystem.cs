// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Whitelist;

namespace Content.Shared.SS220.SiliconComponents;

public sealed partial class SiliconModuleSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool CanInsertModule(Entity<SiliconComponentsComponent> user, Entity<SiliconModuleComponent> module)
    {
        if (!TryComp<SiliconModuleBlacklistComponent>(module, out var whitelistComp))
            return false;

        if (user.Comp.ModuleContainer == null)
            return false;

        foreach (var mod in user.Comp.ModuleContainer.ContainedEntities)
        {
            if (whitelistComp != null &&
                _whitelist.IsWhitelistPass(whitelistComp.ModuleBlacklist, mod))
                return false;

            if (TryComp<SiliconModuleBlacklistComponent>(mod, out var modWhitelistComp) &&
                _whitelist.IsWhitelistPass(modWhitelistComp.ModuleBlacklist, module))
                return false;
        }


        return true;
    }

}

[ByRefEvent]
public record struct SiliconModuleGotInserted(EntityUid Owner)
{
}

[ByRefEvent]
public record struct SiliconModuleGotRemoved(EntityUid Owner)
{
}

[ByRefEvent]
public record struct SiliconModuleInserted(EntityUid Module)
{
}

[ByRefEvent]
public record struct SiliconModuleRemoved(EntityUid Module)
{
}


