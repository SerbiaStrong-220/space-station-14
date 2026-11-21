// using Content.Server.ExternalSources.Frontier.Language;
// using Content.Shared.ExternalSources.Frontier.Language;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Roles;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress;

public sealed partial class RatvarProgressSystem
{
    // [ValidatePrototypeId<LanguagePrototype>]
    // private const string LanguagePrototype = "Ratvar";

    // [Dependency] private readonly LanguageSystem _languageSystem = default!;

    public void SetupRighteous(EntityUid uid)
    {
        // _languageSystem.AddLanguage(uid, LanguagePrototype, true, true);
        if (_progressEntity?.Comp is not { } comp)
            return;

        AddObjectivesToRighteous(
            uid,
            comp.RatvarBeaconsObjective,
            comp.RatvarConvertObjective,
            comp.RatvarPowerObjective,
            comp.RatvarSummonObjective
        );
    }

    private bool CanUseRatvarItems(EntityUid uid)
    {
        return HasComp<RatvarRighteousComponent>(uid);
    }
}
