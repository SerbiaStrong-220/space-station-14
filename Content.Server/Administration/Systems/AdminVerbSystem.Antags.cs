using System.Linq;
using Content.Server.Administration.Commands;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Implants;
using Content.Server.Roles;
using Content.Server.Zombies;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.SS220.Contractor;
using Content.Shared.Store.Components;
using Robust.Shared.Map;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly ImplanterSystem _implant = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultTraitorRule = "Traitor";

    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultInitialInfectedRule = "Zombie";

    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultNukeOpRule = "LoneOpsSpawn";

    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultRevsRule = "Revolutionary";

    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultThiefRule = "Thief";

    [ValidatePrototypeId<StartingGearPrototype>]
    private const string PirateGearId = "PirateGear";

    //SS200 CultYogg start
    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultCultYoggRule = "CultYoggRule";
    //SS220 CultYogg end

    // All antag verbs have names so invokeverb works.
    private void AddAntagVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        if (!HasComp<MindContainerComponent>(args.Target) || !TryComp<ActorComponent>(args.Target, out var targetActor))
            return;

        var targetPlayer = targetActor.PlayerSession;

        Verb traitor = new()
        {
            Text = Loc.GetString("admin-verb-text-make-traitor"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Wallmounts/posters.rsi"), "poster5_contraband"),
            Act = () =>
            {
                _antag.ForceMakeAntag<TraitorRuleComponent>(targetPlayer, DefaultTraitorRule);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-traitor"),
        };
        args.Verbs.Add(traitor);

        //ss220 add verb for shitspawn contractor start
        Verb contractor = new()
        {
            Text = Loc.GetString("admin-verb-make-contractor"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Wallmounts/posters.rsi"), "poster2_legit"),
            Act = () =>
            {
                if (!_mindSystem.TryGetMind(targetPlayer, out var mindId, out var mind))
                    return;

                if (!_role.MindHasRole<TraitorRoleComponent>(mindId))
                {
                    _antag.ForceMakeAntag<TraitorRuleComponent>(targetPlayer, DefaultTraitorRule);
                }

                var uplink = _uplink.FindUplinkTarget(targetPlayer.AttachedEntity!.Value);

                if (uplink is null)
                {
                    if (!TryComp<ImplantedComponent>(targetPlayer.AttachedEntity.Value, out var implanted))
                        return;

                    uplink = implanted.ImplantContainer.ContainedEntities
                        .FirstOrDefault(entity => Prototype(entity)?.ID == "UplinkImplant");
                }

                var uplinkComp = EnsureComp<StoreComponent>(uplink.Value);
                uplinkComp.Balance["Telecrystal"] -= FixedPoint2.New(20);

                EnsureComp<ContractorComponent>(targetPlayer.AttachedEntity!.Value);

                var box = Spawn("BoxContractor", MapCoordinates.Nullspace);
                uplinkComp.FullListingsCatalog.FirstOrDefault(boxContractor => boxContractor.ID == "UplinkContractor")!.PurchaseAmount++;
                _handsSystem.PickupOrDrop(targetPlayer.AttachedEntity.Value, box);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-contractor"),
        };
        args.Verbs.Add(contractor);
        //ss220 add verb for shitspawn contractor end

        Verb initialInfected = new()
        {
            Text = Loc.GetString("admin-verb-text-make-initial-infected"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "InitialInfected"),
            Act = () =>
            {
                _antag.ForceMakeAntag<ZombieRuleComponent>(targetPlayer, DefaultInitialInfectedRule);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-initial-infected"),
        };
        args.Verbs.Add(initialInfected);

        Verb zombie = new()
        {
            Text = Loc.GetString("admin-verb-text-make-zombie"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/Actions/zombie-turn.png")),
            Act = () =>
            {
                _zombie.ZombifyEntity(args.Target);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-zombie"),
        };
        args.Verbs.Add(zombie);


        Verb nukeOp = new()
        {
            Text = Loc.GetString("admin-verb-text-make-nuclear-operative"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Structures/Wallmounts/signs.rsi"), "radiation"),
            Act = () =>
            {
                _antag.ForceMakeAntag<NukeopsRuleComponent>(targetPlayer, DefaultNukeOpRule);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-nuclear-operative"),
        };
        args.Verbs.Add(nukeOp);

        Verb pirate = new()
        {
            Text = Loc.GetString("admin-verb-text-make-pirate"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Clothing/Head/Hats/pirate.rsi"), "icon"),
            Act = () =>
            {
                // pirates just get an outfit because they don't really have logic associated with them
                SetOutfitCommand.SetOutfit(args.Target, PirateGearId, EntityManager);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-pirate"),
        };
        args.Verbs.Add(pirate);

        Verb headRev = new()
        {
            Text = Loc.GetString("admin-verb-text-make-head-rev"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "HeadRevolutionary"),
            Act = () =>
            {
                _antag.ForceMakeAntag<RevolutionaryRuleComponent>(targetPlayer, DefaultRevsRule);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-head-rev"),
        };
        args.Verbs.Add(headRev);

        Verb thief = new()
        {
            Text = Loc.GetString("admin-verb-text-make-thief"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Hands/Gloves/Color/black.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<ThiefRuleComponent>(targetPlayer, DefaultThiefRule);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-thief"),
        };
        args.Verbs.Add(thief);

        //SS220 CultYogg start
        Verb cult_yogg = new()
        {
            Text = Loc.GetString("admin-verb-text-make-cult-yogg"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/SS220/Interface/Misc/cult_yogg_icons.rsi"), "cult_make_yogg"),
            Act = () =>
            {
                _antag.ForceMakeAntag<CultYoggRuleComponent>(targetPlayer, DefaultCultYoggRule);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-cult-yogg"),
        };
        args.Verbs.Add(cult_yogg);
        //SS220 CultYogg end
    }
}
