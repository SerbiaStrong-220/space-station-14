using Content.Server.Humanoid;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SlimePersonColorApplier;

public sealed class SlimePersonColorApplierSystem : EntitySystem
{
    private bool _defaultSlimeColorSet = false;
    private Color _defaultSlimeColor;

    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HumanoidAppearanceComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionChanged(Entity<HumanoidAppearanceComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (!entity.Comp.Species.Equals("SlimePerson"))
            return;

        if (!args.SolutionId.Equals("chemicals"))
            return;

        if (!_defaultSlimeColorSet)
        {
            _defaultSlimeColor = entity.Comp.SkinColor;
            _defaultSlimeColorSet = true;
        }

        // Если реагентов нет, устанавливаем дефолтный цвет, иначе красим в цвет раствора
        var primaryReagent = args.Solution.GetPrimaryReagentId();
        if (primaryReagent == null)
            _humanoid.SetSkinColor(entity, _defaultSlimeColor);
        else
        {
            // Цвет раствора
            Color solutionColor = args.Solution.GetColor(_prototypeManager);
            var colorMultiplier = args.Solution.Volume.Float() / args.Solution.MaxVolume.Float();
            // Множим на multiplier, чтобы получить оттенок в зависимости от количества раствора
            Color appliedColor = new(solutionColor.R * colorMultiplier, solutionColor.G * colorMultiplier, solutionColor.B * colorMultiplier, solutionColor.A);
            // Складываем с основным цветом слаймолюда, чтобы получить итоговый
            Color finalColor = new(Math.Clamp(appliedColor.R + _defaultSlimeColor.R, 0, 255), Math.Clamp(appliedColor.G + _defaultSlimeColor.G, 0, 255), Math.Clamp(appliedColor.B + _defaultSlimeColor.B, 0, 255), _defaultSlimeColor.A);
            _humanoid.SetSkinColor(entity, finalColor);
        }
    }
}
