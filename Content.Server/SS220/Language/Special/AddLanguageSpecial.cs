// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Roles;
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.SS220.Language.Special;
/// <summary>
///     Type for giving language to individual job roles
/// </summary>
[UsedImplicitly]
public sealed partial class AddLanguageSpecial : JobSpecial
{
    [DataField]
    public List<ProtoId<LanguagePrototype>> Languages { get; private set; } = new();

    public override void AfterEquip(EntityUid uid)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var languageSystem = entMan.System<LanguageSystem>();
        var languageComp = entMan.EnsureComponent<LanguageComponent>(uid);
        var languages = Languages.Select(l => l.Id).ToList();
        languageSystem.AddLanguages((uid, languageComp), languages, true);
    }
}
