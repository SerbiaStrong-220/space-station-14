// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Robust.Client.GameObjects;
using Content.Shared.SS220.SuperMatter.Emitter;
using Content.Client.SS220.UserInterface.PlotFigure;

namespace Content.Client.SS220.SuperMatter.Emitter;

public sealed class SuperMatterEmitterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuperMatterEmitterBoltComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<SuperMatterEmitterBoltComponent> entity, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(entity.Owner, out var spriteComponent))
            return;
        spriteComponent.Color = Colormaps.Jet.GetCorrespondingColor(entity.Comp.MatterEnergyRatio);
        spriteComponent.Scale = new Vector2(entity.Comp.PowerConsumedToNormal);
    }
}
