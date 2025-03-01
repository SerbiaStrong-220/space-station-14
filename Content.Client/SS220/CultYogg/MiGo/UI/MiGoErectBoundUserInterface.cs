// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Content.Shared.SS220.CultYogg.Buildings;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Client.Placement;
using Robust.Client.Placement.Modes;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.SS220.CultYogg.MiGo.UI;

[UsedImplicitly]
public sealed class MiGoErectBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlacementManager _placementManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    [ViewVariables]
    private MiGoErectMenu? _menu;
    private EntityUid _owner;
    private PlacementInformation? _placementInformation;
    private ErectMenuState _state;

    public MiGoErectBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();

        _menu = new(this);

        _menu.OnClose += Close;
        _menu.OpenCenteredLeft();

        _placementManager.PlacementChanged += OnPlacementChanged;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        SetState(ErectMenuState.None());
        _menu?.Close();
        _placementManager.PlacementChanged -= OnPlacementChanged;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MiGoErectBuiState msg)
            return;

        _menu?.Update(_owner, msg);
    }

    public void OnBuildingToggle(CultYoggBuildingPrototype building)
    {
        if (_state.MatchBuilding(out var currentBuilding) && currentBuilding == building)
        {
            SetState(ErectMenuState.None());
        }
        else
        {
            SetState(ErectMenuState.Building(building));
        }
    }

    public void OnEraseToggle(bool isErase)
    {
        SetState(isErase ? ErectMenuState.Erase() : ErectMenuState.None());
    }

    public void SendBuildMessage(CultYoggBuildingPrototype building, EntityCoordinates location, Direction direction)
    {
        SendMessage(new MiGoErectBuildMessage()
        {
            BuildingId = building.ID,
            Location = _entityManager.GetNetCoordinates(location),
            Direction = direction,
        });
    }

    public void SendEraseMessage(EntityUid entity)
    {
        SendMessage(new MiGoErectEraseMessage()
        {
            BuildingFrame = _entityManager.GetNetEntity(entity),
        });
    }

    private void SetState(ErectMenuState state)
    {
        var prevState = _state;
        _state = ErectMenuState.None();
        ExitState(prevState);
        _state = state;
        EnterState(_state);
    }

    private void EnterState(ErectMenuState state)
    {
        if (state.MatchBuilding(out var building))
        {
            _menu?.SetSelectedItem(building);
            ActivatePlacement(building);
        }
        else if (state.MatchErase())
        {
            _menu?.SetEraseEnabled(true);
            ActivatePlacement(null);
        }
    }

    private void ExitState(ErectMenuState state)
    {
        if (state.MatchBuilding(out _))
        {
            _menu?.SetSelectedItem(null);
            ClearPlacement();
        }
        else if (state.MatchErase())
        {
            _menu?.SetEraseEnabled(false);
            ClearPlacement();
        }
    }

    private void OnPlacementChanged(object? sender, EventArgs e)
    {
        if (IsMyPlacementActive()) // In this context, this will be true if our placement was disabled
        {
            SetState(ErectMenuState.None());
        }
    }

    private bool IsMyPlacementActive()
    {
        return _placementManager.CurrentPermission == _placementInformation;
    }

    private void ActivatePlacement(CultYoggBuildingPrototype? building)
    {
        var hijack = new MiGoErectPlacementHijack(this, building);
        if (building != null)
        {
            _placementInformation = new PlacementInformation
            {
                IsTile = false,
                Uses = 1,
                PlacementOption = typeof(SnapgridCenter).Name, // API forces this hack
            };
            _placementManager.BeginPlacing(_placementInformation, hijack);
        }
        else
        {
            _placementManager.ToggleEraserHijacked(hijack);
        }
    }

    private void ClearPlacement()
    {
        if (!IsMyPlacementActive())
            return;
        _placementManager.Clear();
        _placementInformation = null;
    }

    // FINITE-STATE MACHINE MY ASS
    private readonly struct ErectMenuState
    {
        private ErectMenuState(Key key, CultYoggBuildingPrototype? building = null)
        {
            _key = key;
            _building = building;
        }

        public static ErectMenuState None() => new();
        public static ErectMenuState Building(CultYoggBuildingPrototype building) => new(Key.Building, building);
        public static ErectMenuState Erase() => new(Key.Erase);

        public bool MatchNone()
        {
            return _key == Key.None;
        }

        public bool MatchBuilding([NotNullWhen(true)] out CultYoggBuildingPrototype? building)
        {
            building = _building;
            return _key == Key.Building;
        }

        public bool MatchErase()
        {
            return _key == Key.Erase;
        }

        private readonly Key _key;
        private readonly CultYoggBuildingPrototype? _building;

        private enum Key
        {
            None,
            Building,
            Erase
        }
    }
}
