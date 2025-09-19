// Original code licensed under Imperial CLA.
// Copyright holders: orix0689 (discord) and pocchitsu (discord)
// See Content.Shared/SS220/Ninja/Objectives/LeaveNoTrace/license.txt

// Modified and/or redistributed under SS220 CLA with hosting restrictions.
// Full license text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.SS220.Ninja.Objectives.LeaveNoTrace;
using Content.Shared.SS220.Ninja.Objectives.LeaveNoTrace.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Ninja.Objectives.LeaveNoTrace;

public sealed class LeaveNoTraceSystem : SharedLeaveNoTraceSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private RevealOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new RevealOverlay();

        SubscribeLocalEvent<LeaveNoTraceComponent, NinjaHideEvent>(OnHide);
        SubscribeLocalEvent<LeaveNoTraceComponent, NinjaRevealedEvent>(OnRevealed);
        SubscribeLocalEvent<LeaveNoTraceComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<LeaveNoTraceComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LeaveNoTraceComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateRevealAnimation();
        UpdateFadeRevealText();
    }

    #region Event Handlers

    private void OnRevealed(Entity<LeaveNoTraceComponent> ent, ref NinjaRevealedEvent args)
    {
        if (_playerManager.LocalEntity != ent.Owner)
            return;

        SetupOverlay(ent.Comp);
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnHide(Entity<LeaveNoTraceComponent> ent, ref NinjaHideEvent args)
    {
        if (_playerManager.LocalEntity != ent.Owner)
            return;

        _overlayManager.RemoveOverlay(_overlay);
        ResetOverlay();
    }

    private void OnShutdown(Entity<LeaveNoTraceComponent> ent, ref ComponentShutdown args)
    {
        if (_playerManager.LocalEntity != ent.Owner)
            return;

        _overlay.IsReveal = true;

        var fadeComponent = EnsureComp<RevealOverlayFadeComponent>(ent.Owner);
        fadeComponent.RemoveRevealOverlayEndTime = _timing.CurTime + fadeComponent.RemoveRevealOverlayTime;
    }

    private void OnPlayerAttached(Entity<LeaveNoTraceComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        if (!ent.Comp.IsSeen)
            return;

        if (_playerManager.LocalEntity != ent.Owner)
            return;

        if (!_overlayManager.AllOverlays.Contains(_overlay))
            return;

        SetupOverlay(ent.Comp);
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<LeaveNoTraceComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        if (_playerManager.LocalEntity != ent.Owner)
            return;

        _overlayManager.RemoveOverlay(_overlay);
        ResetOverlay();
    }

    #endregion

    #region Helpers

    private void UpdateRevealAnimation()
    {
        if (!TryComp<LeaveNoTraceComponent>(_playerManager.LocalEntity, out var component))
            return;

        if (component.RevealEndTime == null) // Paranoia
            return;

        if (!component.IsSeen)
            return;

        _overlay.RevealProgress = 1.0f - (float)(component.RevealEndTime.Value - _timing.CurTime).TotalSeconds / (float)component.TimeForReveal.TotalSeconds;
    }

    private void UpdateFadeRevealText()
    {
        if (!TryComp<RevealOverlayFadeComponent>(_playerManager.LocalEntity, out var component))
            return;

        _overlay.FadeProgress = 1.0f - (float)(component.RemoveRevealOverlayEndTime - _timing.CurTime).TotalSeconds / (float)component.RemoveRevealOverlayTime.TotalSeconds;

        if (_timing.CurTime <= component.RemoveRevealOverlayEndTime)
            return;

        _overlayManager.RemoveOverlay(_overlay);

        ResetOverlay();
        RemComp<RevealOverlayFadeComponent>(_playerManager.LocalEntity.Value);
    }

    private void ResetOverlay()
    {
        _overlay.IsReveal = false;
        _overlay.RevealProgress = 0.0f;
    }

    private void SetupOverlay(LeaveNoTraceComponent component)
    {
        _overlay.RevealLetter = Loc.GetString(component.RevealText);
        _overlay.TextGlitchEffectParams = component.TextGlitchEffectParams;
        _overlay.TextureParams = component.TextureParams;
    }

    #endregion
}
