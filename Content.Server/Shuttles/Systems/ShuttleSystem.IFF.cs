using Content.Server.Shuttles.Components;
using Content.Shared.CCVar;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    private void InitializeIFF()
    {
        SubscribeLocalEvent<IFFConsoleComponent, AnchorStateChangedEvent>(OnIFFConsoleAnchor);
        SubscribeLocalEvent<IFFConsoleComponent, IFFShowIFFMessage>(OnIFFShow);
        SubscribeLocalEvent<IFFConsoleComponent, MapInitEvent>(OnInitIFFConsole);
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
    }

    private void OnGridSplit(ref GridSplitEvent ev)
    {
        var splitMass = _cfg.GetCVar(CCVars.HideSplitGridsUnder);

        if (splitMass < 0)
            return;

        foreach (var grid in ev.NewGrids)
        {
            if (!_physicsQuery.TryGetComponent(grid, out var physics) ||
                physics.Mass > splitMass)
            {
                continue;
            }

            AddIFFFlag(grid, IFFFlags.HideLabel);
        }
    }

    private void OnIFFShow(EntityUid uid, IFFConsoleComponent component, IFFShowIFFMessage args)
    {
        if (!TryComp(uid, out TransformComponent? xform) || xform.GridUid == null)
        {
            return;
        }

        if (!args.Show)
        {
            AddAllSupportedIFFFlags(xform, component);
        }
        else
        {
            RemoveIFFFlag(xform.GridUid.Value, IFFFlags.HideLabel);
            RemoveIFFFlag(xform.GridUid.Value, IFFFlags.Hide);
        }
    }

    private void OnInitIFFConsole(EntityUid uid, IFFConsoleComponent component, MapInitEvent args)
    {
        if (!TryComp(uid, out TransformComponent? xform) || xform.GridUid == null)
        {
            return;
        }

        if (component.HideOnInit)
        {
            AddAllSupportedIFFFlags(xform, component);
        }
    }

    private void OnIFFConsoleAnchor(EntityUid uid, IFFConsoleComponent component, ref AnchorStateChangedEvent args)
    {
        // If we anchor / re-anchor then make sure flags up to date.
        if (!args.Anchored ||
            !TryComp(uid, out TransformComponent? xform) ||
            !TryComp<IFFComponent>(xform.GridUid, out var iff))
        {
            _uiSystem.SetUiState(uid, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = component.AllowedFlags,
                Flags = IFFFlags.None,
            });
        }
        else
        {
            _uiSystem.SetUiState(uid, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = component.AllowedFlags,
                Flags = iff.Flags,
            });
        }
    }

    protected override void UpdateIFFInterfaces(EntityUid gridUid, IFFComponent component)
    {
        base.UpdateIFFInterfaces(gridUid, component);

        var query = AllEntityQuery<IFFConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (xform.GridUid != gridUid)
                continue;

            _uiSystem.SetUiState(uid, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = comp.AllowedFlags,
                Flags = component.Flags,
            });
        }
    }

    // Made this method to avoid copy and pasting.
    /// <summary>
    /// Adds all IFF flags that are allowed by AllowedFlags to the grid.
    /// </summary>
    private void AddAllSupportedIFFFlags(TransformComponent xform, IFFConsoleComponent component)
    {
        if (xform.GridUid == null)
        {
            return;
        }

        if ((component.AllowedFlags & IFFFlags.HideLabel) != 0x0)
        {
            AddIFFFlag(xform.GridUid.Value, IFFFlags.HideLabel);
        }
        if ((component.AllowedFlags & IFFFlags.Hide) != 0x0)
        {
            AddIFFFlag(xform.GridUid.Value, IFFFlags.Hide);
        }
    }

    //ss220 spacewar begin
    public void UpdateIFF()
    {
        var curtime = _gameTiming.CurTime;
        var consoles = EntityQueryEnumerator<IFFConsoleComponent, TransformComponent>();
        while (consoles.MoveNext(out var consoleEnt,out var iffConsole, out var transform))
        {
            if (!TryComp<IFFComponent>(transform.GridUid, out var iff))
                continue;

            if (curtime > iffConsole.StealthUntil && iffConsole.StealthUntil != TimeSpan.Zero)
                RemoveIFFFlag(transform.GridUid.Value, IFFFlags.Hide, iff);

            if (iffConsole.StealthCooldown == TimeSpan.Zero)
                continue;

            SendIFFConsoleState(consoleEnt, iffConsole, iff);
        }
    }

    private TimeSpan GetRemainingTime(TimeSpan until, TimeSpan curtime)
    {
        return until > curtime ? until - curtime : TimeSpan.Zero;
    }

    private void SendIFFConsoleState(EntityUid consoleEnt, IFFConsoleComponent iffConsole, IFFComponent? iff)
    {
        var curtime = _gameTiming.CurTime;

        _uiSystem.SetUiState(consoleEnt, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
        {
            AllowedFlags = iffConsole.AllowedFlags,
            Flags = iff?.Flags ?? iffConsole.AllowedFlags,
            Cooldown = GetRemainingTime(iffConsole.CooldownUntil, curtime),
            StealthDuration = GetRemainingTime(iffConsole.StealthUntil, curtime),
        });
    }
    //ss220 spacewar end
}
