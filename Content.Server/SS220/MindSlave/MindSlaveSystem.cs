// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Antag;
using Content.Server.Mind;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Roles;
using Content.Shared.SS220.MindSlave;
using Content.Shared.Tag;
using Robust.Shared.Audio;

namespace Content.Server.SS220.MindSlave;

public sealed class MindSlaveSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;

    [ValidatePrototypeId<AntagPrototype>]
    private const string MindSlaveAntagId = "MindSlave";

    [ValidatePrototypeId<NpcFactionPrototype>]
    private const string NanoTransenFactionId = "NanoTransen";

    [ValidatePrototypeId<NpcFactionPrototype>]
    private const string SyndicateFactionId = "Syndicate";

    private readonly SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");

    /// <summary>
    /// Dictionary, containing list of all enslaved minds (as a key), and their master (as a value).
    /// </summary>
    public Dictionary<EntityUid, EntityUid> MindSlaves { get; private set; } = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindSlaveComponent, MapInitEvent>(OnMindSlaveImplanted);
        SubscribeLocalEvent<SubdermalImplantComponent, MindSlaveRemoved>(OnMindSlaveRemoved);
    }

    private void OnMindSlaveImplanted(Entity<MindSlaveComponent> entity, ref MapInitEvent args)
    {
        Log.Debug("lmaooooo slave");
    }

    private void OnMindSlaveRemoved(Entity<SubdermalImplantComponent> mind, ref MindSlaveRemoved args)
    {
        if (args.Slave == null)
            return;

        TryRemoveSlave(args.Slave.Value);
    }

    /// <summary>
    /// Makes entity a slave, converting it into an antag.
    /// </summary>
    /// <param name="slave">Entity to be enslaved.</param>
    /// <param name="master">Master of the given entity.</param>
    /// <returns>Whether enslaving were succesfull.</returns>
    public bool TryMakeSlave(EntityUid slave, EntityUid master)
    {
        if (!_mind.TryGetMind(slave, out var mindId, out var mindComp))
            return false;

        if (!_mind.TryGetMind(master, out var masterMindId, out var masterMindComp))
            return false;

        if (HasComp<MindShieldComponent>(slave))
        {
            _popup.PopupEntity(Loc.GetString("mindslave-target-mindshielded"), slave, master);
            return false;
        }

        //Assign role to the mind
        _role.MindAddRole(mindId, new MindSlaveComponent
        {
            PrototypeId = MindSlaveAntagId,
            masterEntity = master
        }, mindComp, true);

        //Assign briefing
        var masterMindName = masterMindComp.CharacterName ?? Loc.GetString("mindslave-unknown-master");
        var briefing = Loc.GetString("mindslave-briefing-slave", ("master", masterMindName));
        _antagSelection.SendBriefing(slave, briefing, null, GreetSoundNotification);

        _role.MindAddRole(mindId, new RoleBriefingComponent
        {
            Briefing = briefing.ToString()
        }, mindComp, true);

        //Change the faction
        _npcFaction.RemoveFaction(slave, NanoTransenFactionId, false);
        _npcFaction.AddFaction(slave, SyndicateFactionId);

        MindSlaves.Add(mindId, masterMindId);

        return true;
    }

    /// <summary>
    /// Removes MindSlave from enslaved entity.
    /// </summary>
    /// <param name="slave">Enslaved entity.</param>
    /// <returns>Whether removing MindSlave were succesfull.</returns>
    public bool TryRemoveSlave(EntityUid slave)
    {
        if (!_mind.TryGetMind(slave, out var mindId, out var mindComp))
            return false;

        if (!HasComp<MindSlaveComponent>(mindId))
            return false;

        var briefing = Loc.GetString("mindslave-removed-slave");
        _antagSelection.SendBriefing(slave, briefing, null, null);

        _role.MindRemoveRole<MindSlaveComponent>(mindId);
        _role.MindRemoveRole<RoleBriefingComponent>(mindId);

        _npcFaction.RemoveFaction(slave, SyndicateFactionId, false);
        _npcFaction.AddFaction(slave, NanoTransenFactionId);

        MindSlaves.Remove(mindId);

        return true;
    }

    /// <summary>
    /// Returns whether the given entity is mind-enslaved by someone.
    /// </summary>
    /// <param name="entity">Entity to be checked.</param>
    /// <returns>Whether the entity is mind-enslaved.</returns>
    public bool IsEnslaved(EntityUid entity)
    {
        if (!_mind.TryGetMind(entity, out var mindId, out var mindComp))
            return false;

        return HasComp<MindSlaveComponent>(mindId);
    }
}
