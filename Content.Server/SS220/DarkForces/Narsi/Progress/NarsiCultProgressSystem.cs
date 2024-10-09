using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DarkForces.Narsi.Progress;

public sealed partial class NarsiCultProgressSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly ObjectivesSystem _objectivesSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string NarsiCultProgressHolder = "NarsiCultProgressHolder";

    private Entity<NarsiCultProgressComponent>? _activeProgress;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarsiCultProgressComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<NarsiCultProgressComponent, ComponentRemove>(OnComponentRemove);

        InitializeObjectives();
        InitializeCultists();
        InitializeAbilities();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateObjectives();
    }

    public void CreateProgress()
    {
        var progress = Spawn(NarsiCultProgressHolder);
        var progressComp = EnsureComp<NarsiCultProgressComponent>(progress);

        _activeProgress = (progress, progressComp);
    }

    private void OnComponentInit(EntityUid uid, NarsiCultProgressComponent component, ComponentInit args)
    {
        OnObjectivesInit((uid, component));
    }

    private void OnComponentRemove(EntityUid uid, NarsiCultProgressComponent component, ComponentRemove args)
    {
        foreach (var objective in component.NarsiObjectives.Objectives)
        {
            QueueDel(objective);
        }
    }

    public int GetBloodScore()
    {
        return _activeProgress?.Comp.BloodPoints ?? 0;
    }

    private void SendMessageFromNarsi(Entity<NarsiCultProgressComponent> progress, string message)
    {
        _radio.SendRadioMessage(progress, message, _prototypeManager.Index<RadioChannelPrototype>("NarsiCult"), progress);
    }
}
