using Content.Server.SS220.DarkForces.Narsi.Cultist.Roles;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress.Roles;
using Content.Shared.Roles;
using Content.Shared.SS220.DarkReaper;
using Content.Shared.SS220.MindSlave;

namespace Content.Server.Roles;

public sealed class RoleSystem : SharedRoleSystem
{
    public override void Initialize()
    {
        // TODO make roles entities
        base.Initialize();

        SubscribeAntagEvents<DragonRoleComponent>();
        SubscribeAntagEvents<InitialInfectedRoleComponent>();
        SubscribeAntagEvents<NinjaRoleComponent>();
        SubscribeAntagEvents<NukeopsRoleComponent>();
        SubscribeAntagEvents<RevolutionaryRoleComponent>();
        SubscribeAntagEvents<SubvertedSiliconRoleComponent>();
        SubscribeAntagEvents<TraitorRoleComponent>();
        SubscribeAntagEvents<ZombieRoleComponent>();
        SubscribeAntagEvents<ThiefRoleComponent>();
        //SS220-mindslave
        SubscribeAntagEvents<MindSlaveRoleComponent>();
        //SS220-Dark-Forces begin
        SubscribeAntagEvents<NarsiCultRoleComponent>();
        SubscribeAntagEvents<RatvarRoleComponent>();
        //SS220-Dark-Forces end
        //SS220 DarkReaper
        SubscribeAntagEvents<DarkReaperRoleComponent>();
    }

    public string? MindGetBriefing(EntityUid? mindId)
    {
        if (mindId == null)
            return null;

        var ev = new GetBriefingEvent();
        RaiseLocalEvent(mindId.Value, ref ev);
        return ev.Briefing;
    }
}

/// <summary>
/// Event raised on the mind to get its briefing.
/// Handlers can either replace or append to the briefing, whichever is more appropriate.
/// </summary>
[ByRefEvent]
public sealed class GetBriefingEvent
{
    public string? Briefing;

    public GetBriefingEvent(string? briefing = null)
    {
        Briefing = briefing;
    }

    /// <summary>
    /// If there is no briefing, sets it to the string.
    /// If there is a briefing, adds a new line to separate it from the appended string.
    /// </summary>
    public void Append(string text)
    {
        if (Briefing == null)
        {
            Briefing = text;
        }
        else
        {
            Briefing += "\n" + text;
        }
    }
}
