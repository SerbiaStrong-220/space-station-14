using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Shared.CCVar;
using Content.Shared.SS220.MartialArts;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.Hands
{
    public sealed class ShowHandItemOverlay : Overlay
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IPlayerManager _player = default!; // SS220-MartialArts
        [Dependency] private readonly IResourceCache _resourceCache = default!; // SS220-MartialArts

        private readonly MartialArtsSystem _martial = default!; // SS220-MartialArts

        // SS220-MartialArts-Start
        private static readonly Color MartialArtsIconsModulate = Color.White.WithAlpha(0.75f);
        private static readonly ResPath MartialArtsActionsRsi =
            new ResPath("/Textures/SS220/Interface/Misc/martial_arts_actions.rsi");

        private const float PerformedStepsVerticalMultiplier = 2f;
        private const float PerformedStepsIndexOffset = 1f;
        private const float PerformedStepsYDivisor = 1.8f;
        private const float PerformedStepsHalfDivisor = 2f;
        // SS220-MartialArts-End

        private HandsSystem? _hands;
        private readonly IRenderTexture _renderBackbuffer;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public Texture? IconOverride;
        public EntityUid? EntityOverride;

        public ShowHandItemOverlay()
        {
            IoCManager.InjectDependencies(this);

            _renderBackbuffer = _clyde.CreateRenderTarget(
                (64, 64),
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                new TextureSampleParameters
                {
                    Filter = true
                }, nameof(ShowHandItemOverlay));

            _martial = _entMan.System<MartialArtsSystem>(); // SS220-MartialArts
        }

        protected override void DisposeBehavior()
        {
            base.DisposeBehavior();

            _renderBackbuffer.Dispose();
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (!_cfg.GetCVar(CCVars.HudHeldItemShow))
                return false;

            return base.BeforeDraw(in args);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var mousePos = _inputManager.MouseScreenPosition;

            // Offscreen
            if (mousePos.Window == WindowId.Invalid)
                return;

            var screen = args.ScreenHandle;
            var offset = _cfg.GetCVar(CCVars.HudHeldItemOffset);
            var offsetVec = new Vector2(offset, offset);

            if (IconOverride != null)
            {
                screen.DrawTexture(IconOverride, mousePos.Position - IconOverride.Size / 2 + offsetVec, Color.White.WithAlpha(0.75f));
                return;
            }

            _hands ??= _entMan.System<HandsSystem>();
            var handEntity = _hands.GetActiveHandEntity();

            DrawPerformedSteps(screen, mousePos, offset); // SS220-MartialArts

            if (handEntity == null || !_entMan.TryGetComponent(handEntity, out SpriteComponent? sprite))
                return;

            var halfSize = _renderBackbuffer.Size / 2;
            var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;

            screen.RenderInRenderTarget(_renderBackbuffer, () =>
            {
                screen.DrawEntity(handEntity.Value, halfSize, new Vector2(1f, 1f) * uiScale, Angle.Zero, Angle.Zero, Direction.South, sprite);
            }, Color.Transparent);

            screen.DrawTexture(_renderBackbuffer.Texture, mousePos.Position - halfSize + offsetVec, Color.White.WithAlpha(0.75f));
        }

        // SS220-MartialArts-Start
        private void DrawPerformedSteps(DrawingHandleScreen screen, ScreenCoordinates mousePos, float offset)
        {
            if (_player.LocalEntity != null)
            {
                var combo = _martial.GetPerformedSteps(_player.LocalEntity.Value);

                if (combo is { Count: > 0 })
                {
                    if (!_resourceCache.TryGetResource<RSIResource>(MartialArtsActionsRsi, out var rsiResource))
                    {
                        DebugTools.Assert($"Couldn't get resource for martial arts actions, expected path: {MartialArtsActionsRsi.CanonPath}");
                        return;
                    }

                    var rsiActual = rsiResource.RSI;

                    for (var i = 0; i < combo.Count; i++)
                    {
                        if (!rsiActual.TryGetState(combo[i].ToString().ToLower(), out var state))
                        {
                            DebugTools.Assert($"No RSI state could be found for state \"{combo[i].ToString().ToLower()}\" in {rsiActual.Path}");
                            continue;
                        }

                        var texture = state.Frame0;

                        var size = texture.Size;

                        var offsetVec2 = new Vector2(-offset,
                            (PerformedStepsVerticalMultiplier * i + PerformedStepsIndexOffset - combo.Count) * texture.Size.Y / PerformedStepsYDivisor);

                        screen.DrawTextureRect(texture,
                            UIBox2.FromDimensions(mousePos.Position + offsetVec2 - size / PerformedStepsHalfDivisor, size),
                            MartialArtsIconsModulate);
                    }
                }
            }
        }
        // SS220-MartialArts-End
    }
}
