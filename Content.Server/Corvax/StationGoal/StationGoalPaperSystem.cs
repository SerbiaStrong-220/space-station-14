using Content.Server.Fax;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Paper;
using Content.Shared.SS220.Photocopier;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Corvax.StationGoal
{
    /// <summary>
    ///     System to spawn paper with station goal.
    /// </summary>
    public sealed class StationGoalPaperSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly FaxSystem _fax = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        }

        private void OnRoundStarted(RoundStartedEvent ev)
        {
            if (!_cfg.GetCVar(CCCVars.StationGoal))
                return;

            var playerCount = _playerManager.PlayerCount;

            var query = EntityQueryEnumerator<StationGoalComponent>();
            while (query.MoveNext(out var uid, out var station))
            {
                var tempGoals = new List<ProtoId<StationGoalPrototype>>(station.Goals);
                StationGoalPrototype? selGoal = null;
                while (tempGoals.Count > 0)
                {
                    var goalId = _random.Pick(tempGoals);
                    var goalProto = _proto.Index(goalId);

                    if (playerCount > goalProto.MaxPlayers ||
                        playerCount < goalProto.MinPlayers)
                    {
                        tempGoals.Remove(goalId);
                        continue;
                    }

                    selGoal = goalProto;
                    break;
                }

                if (selGoal is null)
                    return;

                if (SendStationGoal(uid, selGoal))
                {
                    Log.Info($"Goal {selGoal.ID} has been sent to station {MetaData(uid).EntityName}");
                }
            }
        }

        public bool SendStationGoal(EntityUid ent, ProtoId<StationGoalPrototype> goal)
        {
            return SendStationGoal(ent, _proto.Index(goal));
        }

        /// <summary>
        ///     Send a station goal on selected station to all faxes which are authorized to receive it.
        /// </summary>
        /// <returns>True if at least one fax received paper</returns>
        public bool SendStationGoal(EntityUid ent, StationGoalPrototype goal)
        {
            // SS220 Photocopy begin
            //var printout = new FaxPrintout(
            //    Loc.GetString(goal.Text, ("station", MetaData(ent).EntityName), ("rand_planet_name", GetRandomPlanetName())), //SS220 Random planet name
            //    Loc.GetString("station-goal-fax-paper-name"),
            //    null,
            //    null,
            //    "paper_stamp-centcom",
            //    [new() { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#006600") }]
            //);
            if (!TryComp<StationDataComponent>(ent, out var stationData))
                return false;

            var dataToCopy = new Dictionary<Type, IPhotocopiedComponentData>();
            var paperDataToCopy = new PaperPhotocopiedData()
            {
                Content = Loc.GetString(goal.Text, ("station", MetaData(ent).EntityName), ("rand_planet_name", GetRandomPlanetName())), //SS220 Random planet name
                StampState = "paper_stamp-centcom",
                StampedBy = [
                    new()
                    {
                        StampedName = Loc.GetString("stamp-component-stamped-name-centcom"),
                        StampedColor = Color.FromHex("#dca019") //SS220-CentcomFashion-Changed the stamp color
                    }
                ]
            };
            dataToCopy.Add(typeof(PaperComponent), paperDataToCopy);

            var metaData = new PhotocopyableMetaData()
            {
                EntityName = Loc.GetString("station-goal-fax-paper-name"),
                PrototypeId = "PaperNtFormCc"
            };

            var printout = new PhotocopyableFaxPrintout(dataToCopy, metaData);
            // SS220 Photocopy end

            var wasSent = false;
            var query = EntityQueryEnumerator<FaxMachineComponent>();
            while (query.MoveNext(out var faxUid, out var fax))
            {
                if (!fax.ReceiveAllStationGoals && !(fax.ReceiveStationGoal && _station.GetOwningStation(faxUid) == ent))
                    continue;

                _fax.Receive(faxUid, printout, null, fax);

                foreach (var spawnEnt in goal.Spawns)
                    SpawnAtPosition(spawnEnt, Transform(faxUid).Coordinates);

                wasSent |= fax.ReceiveStationGoal;
            }

            return wasSent;
        }

        //SS220 Random planet name begin
        private string GetRandomPlanetName()
        {
            var rand = new Random();
            string name = $"{(char)rand.Next(65, 90)}-";

            for (var i = 1; i <= 5; i++)
                name += $"{rand.Next(0, 9)}{(i != 5 ? "-" : "")}";

            return name;
        }
        //SS220 Random planet name end
    }
}
