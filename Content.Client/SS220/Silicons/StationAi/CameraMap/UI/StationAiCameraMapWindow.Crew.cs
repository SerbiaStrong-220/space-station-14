// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.Client.Pinpointer.UI;
using Content.Shared.SS220.Silicons.StationAi.CameraMap;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Silicons.StationAi.CameraMap.UI;

public sealed partial class StationAiCameraMapWindow
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private List<StationAiCameraMapCrew> _crew = new();
    private Dictionary<NetEntity, Vector2> _crewPositions = new();

    public event Action<NetEntity>? OnCrewJump;

    private void InitializeCrew()
    {
        CrewSearchLineEdit.OnTextChanged += _ => FilterCrew();
    }

    private void UpdateCrew(StationAiCameraMapState state)
    {
        _crewPositions = state.CrewPositions;

        // Located first, like the crew monitor; positions are not part of the rows, so walking doesn't rebuild them.
        var crew = state.Crew
            .OrderBy(c => !c.Located)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        if (_crew.SequenceEqual(crew))
            return;

        _crew = crew;
        CrewList.DisposeAllChildren();
        var sprites = _entMan.System<SpriteSystem>();
        foreach (var member in _crew)
        {
            var row = new StationAiCrewRow(member, sprites, _prototypes);
            row.OnPressed += _ => OnCrewJump?.Invoke(member.Sensor);
            CrewList.AddChild(row);
        }

        FilterCrew();
    }

    private void FilterCrew()
    {
        var search = CrewSearchLineEdit.Text.Trim();
        var shown = 0;
        var located = 0;
        foreach (var child in CrewList.Children)
        {
            if (child is not StationAiCrewRow row)
                continue;

            var crew = row.Crew;
            row.Visible = crew.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || crew.Job.Contains(search, StringComparison.CurrentCultureIgnoreCase);
            if (!row.Visible)
                continue;

            shown++;
            if (crew.Located)
                located++;
        }

        CrewCount.Text = Loc.GetString("station-ai-camera-map-crew-located", ("count", located));
        CrewEmptyLabel.Visible = shown == 0;
    }

    private void AddCrewBlips()
    {
        foreach (var crew in _crew)
        {
            if (!_crewPositions.TryGetValue(crew.Sensor, out var position))
                continue;

            var coordinates = new EntityCoordinates(_grid, position);
            NavMap.TrackedEntities[crew.Sensor] = new NavMapBlip(coordinates, _blip, StationAiCrewRow.DotColor(crew), false);
            NavMap.Names[crew.Sensor] = Loc.GetString("station-ai-camera-map-crew-row", ("name", crew.Name), ("job", crew.Job));
        }
    }
}
