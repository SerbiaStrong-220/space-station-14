using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.SS220.MindSlave;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Tag;
using Content.Server.SS220.Thermals;

namespace Content.Server.Mindshield;

/// <summary>
/// System used for checking if the implanted is a Rev or Head Rev.
/// </summary>
public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MindSlaveSystem _mindSlave = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _sharedSubdermalImplant = default!;

    [ValidatePrototypeId<TagPrototype>]
    public const string MindShieldTag = "MindShield";

    //SS220-mindslave begin
    [ValidatePrototypeId<TagPrototype>]
    public const string MindSlaveTag = "MindSlave";
    //SS220-mindslave end
    //SS220 Thermal implant begin
    [ValidatePrototypeId<TagPrototype>]
    public const string ThermalImplantTag = "ThermalImplant";
    //SS220 Thermal implant ends

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubdermalImplantComponent, ImplantImplantedEvent>(ImplantCheck);
    }

    /// <summary>
    /// Checks if the implant was a mindshield or not
    /// </summary>
    public void ImplantCheck(EntityUid uid, SubdermalImplantComponent comp, ref ImplantImplantedEvent ev)
    {
        if (_tag.HasTag(ev.Implant, MindShieldTag) && ev.Implanted != null)
        {
            EnsureComp<MindShieldComponent>(ev.Implanted.Value);
            MindShieldRemovalCheck(ev.Implanted.Value, ev.Implant);
        }

        //SS220-mindslave begin
        if (_tag.HasTag(ev.Implant, MindSlaveTag) && ev.Implanted != null && comp.user != null)
        {
            if (!_mindSlave.TryMakeSlave(ev.Implanted.Value, comp.user.Value))
                _sharedSubdermalImplant.ForceRemove(ev.Implanted.Value, ev.Implant);
        }
        //SS220-mindslave end
        //SS220 Thermalvisionimplant begins
        if (_tag.HasTag(ev.Implant, ThermalImplantTag) && ev.Implanted != null)
        {
            EnsureComp<ThermalVisionImplantComponent>(ev.Implanted.Value);
        }
        // else (_tag.HasTag(ev.Implant, ThermalImplantTag) && ev.Implanted != null)
        //SS220 Thermalvisionimplant ends
    }

    /// <summary>
    /// Checks if the implanted person was a Rev or Head Rev and remove role or destroy mindshield respectively.
    /// </summary>
    public void MindShieldRemovalCheck(EntityUid implanted, EntityUid implant)
    {
        if (HasComp<HeadRevolutionaryComponent>(implanted))
        {
            _popupSystem.PopupEntity(Loc.GetString("head-rev-break-mindshield"), implanted);
            QueueDel(implant);
            return;
        }

        if (_mindSystem.TryGetMind(implanted, out var mindId, out _) &&
            _roleSystem.MindTryRemoveRole<RevolutionaryRoleComponent>(mindId))
        {
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(implanted)} was deconverted due to being implanted with a Mindshield.");
        }
    }
}
