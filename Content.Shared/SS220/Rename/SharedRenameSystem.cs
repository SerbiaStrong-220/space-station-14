using Content.Shared.Administration.Managers;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Rename;

public abstract class SharedRenameSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MetaDataComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<MetaDataComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var user = args.User;
        if (!_adminManager.IsAdmin(user))
            return;

        var target = args.Target;

        Verb renameAndRedescribe = new()
        {
            Text = Loc.GetString("admin-verbs-rename-and-redescribe"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/rename_and_redescribe.png")),
            Act = () => Act(user, target),
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-rename-and-redescribe-description"),
        };
        args.Verbs.Add(renameAndRedescribe);
    }

    protected virtual void Act(EntityUid user, EntityUid target) { }
}
