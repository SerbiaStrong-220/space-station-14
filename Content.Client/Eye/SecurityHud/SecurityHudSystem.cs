using Content.Shared.Eye.Blinding.Components;
using Content.Shared.GameTicking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Client.Administration.Managers;
using Content.Client.Administration.Systems;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.SecurityHud.Components;
using Content.Shared.Inventory;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;

namespace Content.Client.Eye.SecurityHud
{
    public sealed class SecurityHudSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IOverlayManager _overlayMan = default!;
        [Dependency] ILightManager _lightManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;


        private SecurityHudOverlay _overlay = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HudableComponent, ComponentInit>(OnSecurityHudInit);
            SubscribeLocalEvent<HudableComponent, ComponentShutdown>(OnSecurityHudShutdown);

            SubscribeLocalEvent<HudableComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<HudableComponent, PlayerDetachedEvent>(OnPlayerDetached);

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartCleanup);
            _overlay = new( EntityManager, _eyeManager, _resourceCache, _entityLookup);
        }

        private void OnPlayerAttached(EntityUid uid, HudableComponent component, PlayerAttachedEvent args)
        {
            _overlayMan.AddOverlay(_overlay);
        }

        private void OnPlayerDetached(EntityUid uid, HudableComponent component, PlayerDetachedEvent args)
        {
            _overlayMan.RemoveOverlay(_overlay);
            _lightManager.Enabled = true;
        }

        private void OnSecurityHudInit(EntityUid uid, HudableComponent component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
                _overlayMan.AddOverlay(_overlay);
        }

        private void OnSecurityHudShutdown(EntityUid uid, HudableComponent component, ComponentShutdown args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _overlayMan.RemoveOverlay(_overlay);
            }
        }

        private void RoundRestartCleanup(RoundRestartCleanupEvent ev)
        {
            _lightManager.Enabled = true;
        }
    }

}
