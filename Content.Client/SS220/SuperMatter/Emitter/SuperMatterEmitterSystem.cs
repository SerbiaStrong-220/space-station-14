using Content.Client.SS220.UserInterface.PlotFigure;
using Content.Shared.Projectiles;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.SS220.SuperMatter.Emitter;
using Robust.Client.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.SS220.SuperMatter.Emitter
{
    public sealed class SuperMatterEmitterSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SpriteSystem _sprite = default!;

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SuperMatterEmitterComponent, AppearanceChangeEvent>(OnAppearanceChange);
            SubscribeLocalEvent<SuperMatterEmitterBoltComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(Entity<SuperMatterEmitterBoltComponent> entity, ref ComponentInit args)
        {
            if (!TryComp<SpriteComponent>(entity.Owner, out var spriteComponent))
                return;
            if (!TryComp<ProjectileComponent>(entity.Owner, out var projectileComponent))
                return;
            var shootAuthorUid = projectileComponent.Shooter;
            if (!TryComp<SuperMatterEmitterComponent>(shootAuthorUid, out var superMatterEmitter))
                return;

            spriteComponent.Color = Colormaps.SMEmitter.GetCorrespondingColor(superMatterEmitter.EnergyToMatterRatio / 100f);
            spriteComponent.Scale = new Vector2(MathF.Sqrt(superMatterEmitter.PowerConsumption / (float)SuperMatterEmitterConsts.BaseEnergyConsumption));
        }

        private void OnAppearanceChange(EntityUid uid, SuperMatterEmitterComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (!_appearance.TryGetData<Shared.SS220.SuperMatter.Emitter.EmitterVisualState>(uid, Shared.SS220.SuperMatter.Emitter.EmitterVisuals.VisualState, out var state, args.Component))
                state = Shared.SS220.SuperMatter.Emitter.EmitterVisualState.Off;

            if (!_sprite.LayerMapTryGet((uid, args.Sprite), Shared.SS220.SuperMatter.Emitter.EmitterVisualLayers.Lights, out var layer, false))
                return;

            switch (state)
            {
                case Shared.SS220.SuperMatter.Emitter.EmitterVisualState.On:
                    if (component.OnState == null)
                        break;
                    _sprite.LayerSetVisible((uid, args.Sprite), layer, true);
                    _sprite.LayerSetRsiState((uid, args.Sprite), layer, component.OnState);
                    break;
                case Shared.SS220.SuperMatter.Emitter.EmitterVisualState.Underpowered:
                    if (component.UnderpoweredState == null)
                        break;
                    _sprite.LayerSetVisible((uid, args.Sprite), layer, true);
                    _sprite.LayerSetRsiState((uid, args.Sprite), layer, component.UnderpoweredState);
                    break;
                case Shared.SS220.SuperMatter.Emitter.EmitterVisualState.Off:
                    _sprite.LayerSetVisible((uid, args.Sprite), layer, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
