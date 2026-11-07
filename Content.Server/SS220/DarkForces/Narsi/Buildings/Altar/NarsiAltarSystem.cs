using Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities;
using Content.Server.SS220.DarkForces.Narsi.Progress;
using Content.Server.Polymorph.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar;

public sealed partial class NarsiAltarSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly NarsiCultProgressSystem _progress = default!;
    [Dependency] private readonly NarsiCultistAbilitiesSystem _abilities = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarsiAltarComponent, GetVerbsEvent<Verb>>(AddVerbToAltar);

        InitPolymorph();
        InitAbilities();
        InitializeRituals();
    }

    private void OpenAltarWindow(EntityUid user, EntityUid altar, NarsiAltarComponent component)
    {
        if (_ui.IsUiOpen(altar, NarsiAltarInterfaceKey.Key))
        {
            _popupSystem.PopupEntity("Алтарь кем-то используется", user, user, PopupType.Medium);
            return;
        }

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (!_ui.HasUi(altar, NarsiAltarInterfaceKey.Key))
            return;

        UpdateAltarState(altar, component);
        _ui.OpenUi(altar, NarsiAltarInterfaceKey.Key, actor.PlayerSession);
    }

    private void UpdateAltarState(EntityUid altar, NarsiAltarComponent component)
    {
        var bloodScore = _progress.GetBloodScore();
        var altarState = new NarsiAltarUIState(bloodScore: bloodScore);

        _ui.SetUiState(altar, NarsiAltarInterfaceKey.Key, altarState);
    }

    private void AddVerbToAltar(EntityUid uid, NarsiAltarComponent component, GetVerbsEvent<Verb> args)
    {
        if (!HasComp<NarsiCultistComponent>(args.User))
            return;

        Verb altarVerb = new()
        {
            Act = () => OpenAltarWindow(args.User, uid, component),
            DoContactInteraction = true,
            Text = "Алтарь Нар'Си",
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/SS220/DarkForces/Cult/Structures/altar.rsi"), "narsi")
        };
        args.Verbs.Add(altarVerb);
    }
}
