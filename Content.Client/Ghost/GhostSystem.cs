using Content.Client.Movement.Systems;
using Content.Shared.Actions;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Ghost;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Sprite;

namespace Content.Client.Ghost
{
    public sealed class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly IClientConsoleHost _console = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
        [Dependency] private readonly ContentEyeSystem _contentEye = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public int AvailableGhostRoleCount { get; private set; }

        //SS220-ghost-hats
        private const string HatEquippedState = "equipped-HELMET";

        private bool _ghostVisibility = true;

        private bool GhostVisibility
        {
            get => _ghostVisibility;
            set
            {
                if (_ghostVisibility == value)
                {
                    return;
                }

                _ghostVisibility = value;

                var query = AllEntityQuery<GhostComponent, SpriteComponent>();
                while (query.MoveNext(out var uid, out _, out var sprite))
                {
                    sprite.Visible = value || uid == _playerManager.LocalEntity;
                }
            }
        }

        public GhostComponent? Player => CompOrNull<GhostComponent>(_playerManager.LocalEntity);
        public bool IsGhost => Player != null;

        public event Action<GhostComponent>? PlayerRemoved;
        public event Action<GhostComponent>? PlayerUpdated;
        public event Action<GhostComponent>? PlayerAttached;
        public event Action? PlayerDetached;
        public event Action<GhostWarpsResponseEvent>? GhostWarpsResponse;
        public event Action<GhostUpdateGhostRoleCountEvent>? GhostRoleCountUpdated;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<GhostComponent, ComponentRemove>(OnGhostRemove);
            SubscribeLocalEvent<GhostComponent, AfterAutoHandleStateEvent>(OnGhostState);

            SubscribeLocalEvent<GhostComponent, LocalPlayerAttachedEvent>(OnGhostPlayerAttach);
            SubscribeLocalEvent<GhostComponent, LocalPlayerDetachedEvent>(OnGhostPlayerDetach);

            SubscribeNetworkEvent<GhostWarpsResponseEvent>(OnGhostWarpsResponse);
            SubscribeNetworkEvent<GhostUpdateGhostRoleCountEvent>(OnUpdateGhostRoleCount);

            SubscribeLocalEvent<EyeComponent, ToggleLightingActionEvent>(OnToggleLighting);
            SubscribeLocalEvent<EyeComponent, ToggleFoVActionEvent>(OnToggleFoV);
            SubscribeLocalEvent<GhostComponent, ToggleGhostsActionEvent>(OnToggleGhosts);
        }

        private void OnStartup(EntityUid uid, GhostComponent component, ComponentStartup args)
        {
            if (TryComp(uid, out SpriteComponent? sprite))
                sprite.Visible = GhostVisibility || uid == _playerManager.LocalEntity;
        }

        private void OnToggleLighting(EntityUid uid, EyeComponent component, ToggleLightingActionEvent args)
        {
            if (args.Handled)
                return;

            TryComp<PointLightComponent>(uid, out var light);

            if (!component.DrawLight)
            {
                // normal lighting
                Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-lighting-manager-popup-normal"), args.Performer);
                _contentEye.RequestEye(component.DrawFov, true);
            }
            else if (!light?.Enabled ?? false) // skip this option if we have no PointLightComponent
            {
                // enable personal light
                Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-lighting-manager-popup-personal-light"), args.Performer);
                _pointLightSystem.SetEnabled(uid, true, light);
            }
            else
            {
                // fullbright mode
                Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-lighting-manager-popup-fullbright"), args.Performer);
                _contentEye.RequestEye(component.DrawFov, false);
                _pointLightSystem.SetEnabled(uid, false, light);
            }
            args.Handled = true;
        }

        private void OnToggleFoV(EntityUid uid, EyeComponent component, ToggleFoVActionEvent args)
        {
            //SS220-colorful-ghosts begin
            if (TryComp<SpriteComponent>(uid, out var sprite))
            {
                var _random = new Random();
                var color = new Color(_random.Next(1, 255), _random.Next(1, 255), _random.Next(1, 255));

                // sprite.Color = color;

                //sprite.Rotation += Angle.FromDegrees(180.0f);  //SS220 Ghost rotation fix

                sprite.Color = sprite.Color.WithBlue(10);
                //var t = sprite.GetType();

                //var pr = t.GetProperties();

                //var col =  pr.FirstOrDefault(x => x.Name == "Color");

                //if (col is not null)
                //{

                //    col.GetSetMethod(true)!.Invoke(sprite, new object[] { color });
                //}

                // sprite.
                // PlayerUpdated?.Invoke(Player);
            }
            //SS220-colorful-ghosts end

            if (args.Handled)
                return;

            Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-fov-popup"), args.Performer);
            _contentEye.RequestToggleFov(uid, component);
            args.Handled = true;
        }

        private void OnToggleGhosts(EntityUid uid, GhostComponent component, ToggleGhostsActionEvent args)
        {
            if (args.Handled)
                return;

            var locId = GhostVisibility ? "ghost-gui-toggle-ghost-visibility-popup-off" : "ghost-gui-toggle-ghost-visibility-popup-on";
            Popup.PopupEntity(Loc.GetString(locId), args.Performer);
            if (uid == _playerManager.LocalEntity)
                ToggleGhostVisibility();

            args.Handled = true;
        }

        //SS220-ghost-hats begin
        private void SetBodyVisuals(EntityUid uid, SpriteComponent? sprite, bool visible)
        {
            if (!Resolve(uid, ref sprite))
                return;

            if (!HasComp<GhostComponent>(uid))
                return;

            var typingIndicatorStates = new string[0];
            if (TryComp<TypingIndicatorComponent>(uid, out var typingIndicator) &&
                _prototypeManager.TryIndex<TypingIndicatorPrototype>(typingIndicator.TypingIndicatorPrototype, out var typingProto))
                typingIndicatorStates = new string[] { typingProto.TypingState, typingProto.IdleState };

            var spriteLayers = sprite.AllLayers;
            foreach (var layer in spriteLayers)
            {
                if (layer.RsiState == HatEquippedState ||
                    typingIndicatorStates.Contains(layer.RsiState.ToString()))
                    continue;

                layer.Visible = visible;
            }
        }
        //SS220-ghost-hats end

        private void OnGhostRemove(EntityUid uid, GhostComponent component, ComponentRemove args)
        {
            _actions.RemoveAction(uid, component.ToggleLightingActionEntity);
            _actions.RemoveAction(uid, component.ToggleFoVActionEntity);
            _actions.RemoveAction(uid, component.ToggleGhostsActionEntity);
            _actions.RemoveAction(uid, component.ToggleGhostHearingActionEntity);
            // SS220 ADD GHOST HUD'S
            _actions.RemoveAction(uid, component.ToggleHudOnOtherActionEntity);
            //SS220-ghost-hats
            _actions.RemoveAction(uid, component.ToggleAGhostBodyVisualsActionEntity);

            if (uid != _playerManager.LocalEntity)
                return;

            ResetGhostVisibility(); // SS220-Fix ghosts visibility for other entities
            PlayerRemoved?.Invoke(component);
        }

        private void OnGhostPlayerAttach(EntityUid uid, GhostComponent component, LocalPlayerAttachedEvent localPlayerAttachedEvent)
        {
            // SS220 colorful ghost begin
            if (TryComp<SpriteComponent>(uid, out var sprite))
            {
                var random = new Random();

                var color = new Color(
                    (float) random.Next(1, 255) / byte.MaxValue,
                    (float) random.Next(1, 255) / byte.MaxValue,
                    (float) random.Next(1, 255) / byte.MaxValue,
                    sprite.Color.A);

                sprite.Color = color;
            }
            // SS220 colorful ghost end

            ResetGhostVisibility(); // SS220-Fix ghosts visibility for other entities
            PlayerAttached?.Invoke(component);
        }

        private void OnGhostState(EntityUid uid, GhostComponent component, ref AfterAutoHandleStateEvent args)
        {
            if (TryComp<SpriteComponent>(uid, out var sprite))
            {
                //SS220-colorful-ghosts
                //sprite.LayerSetColor(0, component.color);

                //SS220-ghost-hats
                SetBodyVisuals(uid, sprite, component.BodyVisible);
            }

            if (uid != _playerManager.LocalEntity)
                return;

            PlayerUpdated?.Invoke(component);
        }

        private void OnGhostPlayerDetach(EntityUid uid, GhostComponent component, LocalPlayerDetachedEvent args)
        {
            ResetGhostVisibility(); // SS220-Fix ghosts visibility for other entities
            PlayerDetached?.Invoke();
        }

        // SS220-Fix ghosts visibility for other entities-begin
        // Just a semantic separation for a better understanding of the logic
        private void ResetGhostVisibility()
        {
            GhostVisibility = true;
        }
        // SS220-Fix ghosts visibility for other entities-end

        private void OnGhostWarpsResponse(GhostWarpsResponseEvent msg)
        {
            if (!IsGhost)
            {
                return;
            }

            GhostWarpsResponse?.Invoke(msg);
        }

        private void OnUpdateGhostRoleCount(GhostUpdateGhostRoleCountEvent msg)
        {
            AvailableGhostRoleCount = msg.AvailableGhostRoles;
            GhostRoleCountUpdated?.Invoke(msg);
        }

        public void RequestWarps()
        {
            RaiseNetworkEvent(new GhostWarpsRequestEvent());
        }

        public void ReturnToBody()
        {
            var msg = new GhostReturnToBodyRequest();
            RaiseNetworkEvent(msg);
        }

        public void OpenGhostRoles()
        {
            _console.RemoteExecuteCommand(null, "ghostroles");
        }

        public void ToggleGhostVisibility(bool? visibility = null)
        {
            GhostVisibility = visibility ?? !GhostVisibility;
        }
    }
}
