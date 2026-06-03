using Content.Shared.Implants;
using Content.Shared.Roles;
using Content.Shared.SS220.MartialArts;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Jobs;


[UsedImplicitly]
public sealed partial class AddMartialArtSpecial : JobSpecial
{
    [DataField]
    public ProtoId<MartialArtPrototype> MartialArt; //Not a list because more than one martial art doesn't make sense

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var martialArtsSystem = entMan.System<MartialArtsSystem>();
        martialArtsSystem.TryGrantMartialArt(mob, MartialArt);
    }
}
