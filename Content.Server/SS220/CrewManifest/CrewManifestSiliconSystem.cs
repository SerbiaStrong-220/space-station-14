// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Silicons.Borgs;
using Content.Server.Station.Systems;
using Content.Shared.SS220.CrewManifest;
using Content.Shared.GameTicking;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CrewManifest;

/// <summary>
///     Collects the synthetics (borgs and station AIs) displayed in the crew manifest.
/// </summary>
public sealed partial class CrewManifestSiliconSystem : EntitySystem
{
    private static readonly ProtoId<JobIconPrototype> StationAiJobIcon = "JobIconStationAi";
    private const string AiChassisLocId = "silicon-manifest-ai-chassis";

    [Dependency] private StationSystem _station = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        SubscribeLocalEvent<StationAiCoreComponent, SiliconManifestCoreChangedEvent>(OnAiCoreChanged);
        SubscribeLocalEvent<EntityRenamedEvent>(OnEntityRenamed);
    }

    public bool IsRegistered(EntityUid borg, EntityUid station)
    {
        return TryComp<SiliconManifestMemberComponent>(borg, out var member) && member.Station == station;
    }

    /// <summary>
    ///     Adds a borg to a station's manifest or removes it.
    /// </summary>
    public void Toggle(EntityUid borg, EntityUid station)
    {
        if (TryComp<SiliconManifestMemberComponent>(borg, out var member) && member.Station == station)
        {
            RemComp<SiliconManifestMemberComponent>(borg);
            RaiseManifestChanged(station);
            return;
        }

        var newMember = EnsureComp<SiliconManifestMemberComponent>(borg);
        newMember.Station = station;
        RaiseManifestChanged(station);
    }

    /// <summary>
    ///     Rebuilds the manifest of the station the borg is listed in.
    /// </summary>
    public void NotifyChanged(EntityUid borg)
    {
        if (!TryComp<SiliconManifestMemberComponent>(borg, out var member) || member.Station == EntityUid.Invalid)
            return;

        RaiseManifestChanged(member.Station);
    }

    private void RaiseManifestChanged(EntityUid station)
    {
        var ev = new SiliconManifestChangedEvent(station);
        RaiseLocalEvent(ref ev);
    }

    /// <summary>
    ///     Builds the synthetic entries for a station's manifest.
    /// </summary>
    public SyntheticManifestEntry[] BuildSynthetics(EntityUid station)
    {
        var entries = new List<SyntheticManifestEntry>();

        var coreQuery = EntityQueryEnumerator<StationAiCoreComponent, StationAiHolderComponent>();
        while (coreQuery.MoveNext(out var coreUid, out _, out var holder))
        {
            if (_station.GetOwningStation(coreUid) != station)
                continue;

            if (!_stationAi.TryGetHeld((coreUid, (StationAiHolderComponent?)holder), out var aiUid))
                continue;

            entries.Add(new SyntheticManifestEntry(
                MetaData(aiUid.Value).EntityName,
                Loc.GetString(AiChassisLocId),
                StationAiJobIcon,
                true));
        }

        var borgs = new List<SyntheticManifestEntry>();
        var borgQuery = EntityQueryEnumerator<SiliconManifestMemberComponent>();
        while (borgQuery.MoveNext(out var borgUid, out var member))
        {
            if (member.Station != station)
                continue;

            borgs.Add(new SyntheticManifestEntry(
                MetaData(borgUid).EntityName,
                GetBorgChassisName(borgUid),
                GetBorgJobIcon(borgUid),
                false));
        }

        borgs.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        entries.AddRange(borgs);

        return entries.ToArray();
    }

    private string GetBorgChassisName(EntityUid borg)
    {
        if (TryComp<BorgSwitchableTypeComponent>(borg, out var switchable))
        {
            if (switchable.SelectedBorgType is not { } borgTypeId ||
                !_prototype.TryIndex(borgTypeId, out BorgTypePrototype? borgType))
            {
                return Loc.GetString("silicon-manifest-chassis-none");
            }

            if (!string.IsNullOrEmpty(borgType.Name))
                return Loc.GetString(borgType.Name);
        }

        if (TryComp<BorgTransponderComponent>(borg, out var transponder) &&
            !string.IsNullOrEmpty(transponder.Name))
        {
            return transponder.Name;
        }

        return Loc.GetString("borg-type-prototype-generic");
    }

    private ProtoId<JobIconPrototype>? GetBorgJobIcon(EntityUid borg)
    {
        if (TryComp<JobStatusComponent>(borg, out var jobStatus))
            return jobStatus.JobStatusIcon;

        return null;
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId != BorgSystem.BorgJobId.Id ||
            ev.Station == EntityUid.Invalid)
        {
            return;
        }

        var member = EnsureComp<SiliconManifestMemberComponent>(ev.Mob);
        member.Station = ev.Station;
        RaiseManifestChanged(ev.Station);
    }

    private void OnAiCoreChanged(Entity<StationAiCoreComponent> ent, ref SiliconManifestCoreChangedEvent args)
    {
        if (_station.GetOwningStation(ent.Owner) is { } station)
            RaiseManifestChanged(station);
    }

    private void OnEntityRenamed(ref EntityRenamedEvent args)
    {
        if (TryComp<SiliconManifestMemberComponent>(args.Uid, out var member))
        {
            if (member.Station != EntityUid.Invalid)
                RaiseManifestChanged(member.Station);

            return;
        }

        if (!HasComp<StationAiHeldComponent>(args.Uid))
            return;

        if (_station.GetOwningStation(args.Uid) is { } station)
            RaiseManifestChanged(station);
    }
}

