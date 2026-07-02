using Content.Server.Shuttles.Systems;
using Content.Server.SS220.Shuttles.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.SS220.Shuttles.BUIStates;
using Content.Shared.SS220.Shuttles.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Shuttles.Systems;

public sealed partial class ComputerIFFEbentConsoleSystem : EntitySystem
{
    [Dependency] private ShuttleSystem _shuttle = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComputerIFFEbentConsoleComponent, ActivateComputerIFFEbentConsoleMessage>(OnActivateStealth);
        SubscribeLocalEvent<ComputerIFFEbentConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ComputerIFFEbentConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<ComputerIFFEbentConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
    }

    public override void Update(float frameTime)
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<ComputerIFFEbentConsoleComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var component, out var transform))
        {
            var changed = false;

            if (component.StealthUntil != TimeSpan.Zero && curTime >= component.StealthUntil)
            {
                if (transform.GridUid is { } gridUid)
                    _shuttle.RemoveIFFFlag(gridUid, component.AllowedFlags);

                component.StealthUntil = TimeSpan.Zero;
                changed = true;
            }

            if (component.CooldownUntil != TimeSpan.Zero && curTime >= component.CooldownUntil)
            {
                component.CooldownUntil = TimeSpan.Zero;
                changed = true;
            }

            if (changed || component.StealthUntil != TimeSpan.Zero || component.CooldownUntil != TimeSpan.Zero)
                UpdateInterface(uid, component);
        }
    }

    private void OnActivateStealth(Entity<ComputerIFFEbentConsoleComponent> ent, ref ActivateComputerIFFEbentConsoleMessage args)
    {
        if (_gameTiming.CurTime < ent.Comp.CooldownUntil)
            return;

        if (!TryComp(ent, out TransformComponent? transform) || transform.GridUid is not { } gridUid)
            return;

        _shuttle.AddIFFFlag(gridUid, ent.Comp.AllowedFlags);
        ent.Comp.StealthUntil = _gameTiming.CurTime + ent.Comp.StealthTime;
        ent.Comp.CooldownUntil = ent.Comp.StealthUntil + ent.Comp.StealthCooldown;

        UpdateInterface(ent, ent.Comp);
    }

    private void OnMapInit(Entity<ComputerIFFEbentConsoleComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.HideOnInit && TryComp(ent, out TransformComponent? transform) && transform.GridUid is { } gridUid)
            _shuttle.AddIFFFlag(gridUid, ent.Comp.AllowedFlags);

        UpdateInterface(ent, ent.Comp);
    }

    private void OnAnchorChanged(Entity<ComputerIFFEbentConsoleComponent> ent, ref AnchorStateChangedEvent args)
    {
        UpdateInterface(ent, ent.Comp);
    }

    private void OnUiOpened(Entity<ComputerIFFEbentConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateInterface(ent, ent.Comp);
    }

    private void UpdateInterface(EntityUid uid, ComputerIFFEbentConsoleComponent component)
    {
        _uiSystem.SetUiState(uid, ComputerIFFEbentConsoleUiKey.Key, new ComputerIFFEbentConsoleBoundUserInterfaceState
        {
            Cooldown = GetRemainingTime(component.CooldownUntil),
            StealthDuration = GetRemainingTime(component.StealthUntil),
        });
    }

    private TimeSpan GetRemainingTime(TimeSpan until)
    {
        var curTime = _gameTiming.CurTime;
        return until > curTime ? until - curTime : TimeSpan.Zero;
    }
}
