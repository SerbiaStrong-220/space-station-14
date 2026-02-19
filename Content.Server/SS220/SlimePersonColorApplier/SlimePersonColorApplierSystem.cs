// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Humanoid;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Color = Robust.Shared.Maths.Color;

namespace Content.Server.SS220.SlimePersonColorApplier;

public sealed class SlimePersonColorApplierSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SlimePersonColorApplierComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<SlimePersonColorApplierComponent, ComponentInit>(OnCompInit);
    }

    private void OnCompInit(Entity<SlimePersonColorApplierComponent> entity, ref ComponentInit args)
    {
        if (TryComp<HumanoidAppearanceComponent>(entity, out var huApComp))
        {
            entity.Comp.BeforeSkinColor = huApComp.SkinColor;
            entity.Comp.BeforeCustomBaseLayers = new(huApComp.CustomBaseLayers);
        }
    }

    private void OnSolutionChanged(Entity<SlimePersonColorApplierComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (!args.SolutionId.Equals("chemicals"))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(entity))
            return;

        var primaryReagent = args.Solution.GetPrimaryReagentId();
        if (primaryReagent == null)
        {
            _humanoid.SetSkinColor(entity, entity.Comp.BeforeSkinColor);
            // _humanoid.SetMarkingColor();
        }
        else
        {
            Color solutionColor = args.Solution.GetColor(_prototypeManager);
            var colorMultiplier = args.Solution.Volume.Float() / args.Solution.MaxVolume.Float();
            Color appliedColor = new(solutionColor.R * colorMultiplier, solutionColor.G * colorMultiplier, solutionColor.B * colorMultiplier, solutionColor.A);
            Color finalColor = Color.Blend(entity.Comp.BeforeSkinColor, appliedColor, Color.BlendFactor.One, Color.BlendFactor.One);
            _humanoid.SetSkinColor(entity, finalColor);
        }
    }
}
