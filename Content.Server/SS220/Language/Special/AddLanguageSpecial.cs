// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language;
using Content.Shared.Roles;
using JetBrains.Annotations;


namespace Content.Server.SS220.Language.Special;
/// <summary>
///     Type for giving language to individual job roles
/// </summary>
[UsedImplicitly]
public sealed partial class AddLanguageSpecial : JobSpecial
{
    [DataField]
    public List<string> Languages { get; private set; } = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var languageSystem = entMan.System<SharedLanguageSystem>();
        languageSystem.AddLanguages(mob, Languages);
    }
}
