using Content.Server.SS220.DarkForces.Narsi.Runes.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.SS220.FastUI;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.SS220.DarkForces.Narsi.Runes;
using NarsiDaggerClearRuneDoAfterEvent =
    Content.Shared.SS220.DarkForces.Narsi.Dagger.NarsiDaggerClearRuneDoAfterEvent;

namespace Content.Server.SS220.DarkForces.Narsi.Dagger;

public sealed class NarsiRitualDaggerSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private static readonly SoundSpecifier BloodSound =
        new SoundPathSpecifier("/Audio/SS220/DarkForces/Cult/enter_blood.ogg");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NarsiRitualDaggerComponent, GetVerbsEvent<Verb>>(AddVerbToRitualDagger);
        SubscribeLocalEvent<NarsiRitualDaggerComponent, SelectItemMessage>(OnSelectedItemMessage);
        SubscribeLocalEvent<NarsiRitualDaggerComponent, UseInHandEvent>(OnUseInHand);

        //for clear runes
        SubscribeLocalEvent<NarsiRitualDaggerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<NarsiRitualDaggerComponent, NarsiDaggerClearRuneDoAfterEvent>(OnClearRuneEvent);
        SubscribeLocalEvent<NarsiRitualDaggerComponent, CreateNarsiRuneDoAfterEvent>(OnCreateRuneEvent);
    }

    private void OnCreateRuneEvent(EntityUid uid, NarsiRitualDaggerComponent component,
        CreateNarsiRuneDoAfterEvent args)
    {
        var transform = Transform(args.User).Coordinates;
        Spawn(args.Prototype, transform.SnapToGrid(EntityManager, _mapManager));
    }

    private void OnClearRuneEvent(EntityUid uid, NarsiRitualDaggerComponent component,
        NarsiDaggerClearRuneDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        args.Handled = true;
        QueueDel(args.Target.Value);
    }

    private void OnUseInHand(EntityUid uid, NarsiRitualDaggerComponent component, UseInHandEvent args)
    {
        if (!HasComp<NarsiCultistComponent>(uid))
            return;

        DrawRune(args.User, uid);
    }

    private void OnAfterInteract(EntityUid uid, NarsiRitualDaggerComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == null || !HasComp<NarsiRuneComponent>(args.Target) ||
            HasComp<NarsiSpawnRuneComponent>(args.Target))
            return;

        args.Handled = true;

        var doAfterEvent = new NarsiDaggerClearRuneDoAfterEvent();
        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            user: args.User,
            delay: TimeSpan.FromSeconds(5),
            @event: doAfterEvent,
            eventTarget: uid,
            target: args.Target,
            used: uid
        )
        {
            BreakOnMove = true,
            MovementThreshold = 1.0f,
        };
        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnSelectedItemMessage(EntityUid uid, NarsiRitualDaggerComponent component, SelectItemMessage args)
    {
        if (args.Key != "NarsiRuneListing")
            return;

        SendCreateNarsiRuneEvent(uid, args.Actor, args.Data.ID);
    }

    private void AddVerbToRitualDagger(EntityUid uid, NarsiRitualDaggerComponent component, GetVerbsEvent<Verb> args)
    {
        if (!HasComp<NarsiCultistComponent>(args.User))
            return;

        Verb runes = new()
        {
            Act = () => DrawRune(args.User, uid),
            DoContactInteraction = true,
            Text = "Руны",
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/SS220/DarkForces/Cult/Entities/Runes/rune.rsi"),
                "offering")
        };

        args.Verbs.Add(runes);
    }

    private void DrawRune(EntityUid user, EntityUid dagger)
    {
        if (!_ui.HasUi(dagger, SecretListingKey.Key))
            return;

        if (!_ui.TryOpenUi(dagger, SecretListingKey.Key, user))
            return;

        var runes = _prototypeManager.Index<SecretListingCategoryPrototype>("NarsiRuneListing");
        var state = new SecretListingInitState(runes, GetNetEntity(user));
        _ui.SetUiState(dagger, SecretListingKey.Key, state);
    }

    private void SendCreateNarsiRuneEvent(EntityUid dagger, EntityUid user, string prototype)
    {
        var doAfterEvent = new CreateNarsiRuneDoAfterEvent(prototype);
        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            user: user,
            delay: TimeSpan.FromSeconds(10),
            @event: doAfterEvent,
            eventTarget: dagger,
            target: null,
            used: dagger
        )
        {
            BreakOnMove = true,
            MovementThreshold = 1.0f,
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        _audioSystem.PlayEntity(BloodSound, Filter.Pvs(user, entityManager: EntityManager), user, true,
            BloodSound.Params);
    }
}
