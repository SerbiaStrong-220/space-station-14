// SS220 Changeling
using Content.Server.Changeling.Components;
using Content.Server.Emp;
using Content.Server.Polymorph.Systems;
using Content.Server.SS220.TTS;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Cloning;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.SS220.Telepathy;
using Content.Shared.Stealth;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Changeling.Systems;

/// <summary>
/// Utility mutations and chemical stings. All effects are authoritative and are torn down when a full
/// evolution reset is requested by devouring a victim.
/// </summary>
public sealed partial class ChangelingUtilityMutationSystem : EntitySystem
{
    private const string DarknessRegenKey = "changeling-darkness-adaptation";
    private const string OrganicSuitRegenKey = "changeling-organic-space-suit";
    private const string StoredSuitOuterClothing = "changeling-stored-suit-outer";
    private const string StoredSuitHead = "changeling-stored-suit-head";
    private const float MaxMutationChemicalCost = 1_000f;
    private static readonly TimeSpan MaxAdaptationInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MaxTransformationWindup = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxTransformationDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CryogenicStingTickInterval = TimeSpan.FromSeconds(2);
    private static readonly SoundPathSpecifier MutationFormSound = new("/Audio/_Goobstation/Changeling/Effects/armour_transform.ogg");
    private static readonly SoundPathSpecifier MutationRetractSound = new("/Audio/_Goobstation/Changeling/Effects/armour_strip.ogg");
    private static readonly SoundPathSpecifier MutationShriekSound = new("/Audio/Voice/Vox/shriek1.ogg");
    private static readonly EntProtoId HumanFormAction = "ActionChangelingHumanForm";
    private static readonly EntProtoId VoidAdaptationAction = "ActionChangelingVoidAdaptation";
    private static readonly ProtoId<PolymorphPrototype> ChangelingLesserForm = "ChangelingLesserForm";
    private static readonly EntProtoId OrganicSpaceSuitVisual = "ChangelingOrganicSpaceSuitVisual";
    private static readonly EntProtoId OrganicSpaceSuitHelmetVisual = "ChangelingOrganicSpaceSuitHelmetVisual";
    private static readonly EntProtoId InfestationSpider = "MobGiantSpiderAngry";
    private static readonly EntProtoId FakeArmBlade = "ChangelingFakeArmBlade";
    private static readonly ProtoId<TelepathyChannelPrototype> HiveTelepathyChannel = "TelepathyChannelHive";
    private static readonly ProtoId<ReagentPrototype> MuteToxin = "MuteToxin";
    private static readonly ProtoId<ReagentPrototype> Nitrogen = "Nitrogen";
    private static readonly ProtoId<ReagentPrototype> Nocturine = "Nocturine";
    private static readonly ProtoId<DamageTypePrototype> ColdDamage = "Cold";
    private const string ChangelingBuiXmlGeneratedName = "ChangelingTransformBoundUserInterface";

    [Dependency] private readonly ChangelingResourceSystem _resources = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _identities = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedCloningSystem _cloning = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly SharedEmpSystem _emp = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly TTSSystem _tts = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IdentitySystem _identitySystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Reusable scratch buffers only. Each synchronous query clears its buffer before use; neither collection
    // represents persistent gameplay or ECS state.
    private readonly Dictionary<string, EntityUid> _hiveGenomes = new();
    private readonly HashSet<Entity<PointLightComponent>> _nearbyLights = new();

    public override void Initialize()
    {
        base.Initialize();

        InitializeAdaptations();
        InitializeForms();
        InitializeOrganicSuit();
        InitializeUtilityAbilities();
        InitializeTransformationSting();
        InitializeChemicalStings();
        InitializeCleanup();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = _timing.CurTime;

        UpdateAdaptations(now);
        UpdateBlindStings(now);
        UpdateCryogenicStings(now);
        UpdateTransformationStings(now);
    }

    private ChangelingUtilityStateComponent EnsureState(Entity<ChangelingResourceComponent> ent)
    {
        return EnsureComp<ChangelingUtilityStateComponent>(ent);
    }

    private bool Spend(EntityUid uid, float amount)
    {
        if (!IsValidChemicalAmount(amount))
            return false;

        if (_resources.TrySpendChemicals(uid, FixedPoint2.New(amount)))
            return true;

        _popup.PopupEntity(Loc.GetString("changeling-not-enough-chemicals"), uid, uid);
        return false;
    }

    private static bool IsValidChemicalAmount(float amount)
    {
        return float.IsFinite(amount) && amount >= 0f && amount <= MaxMutationChemicalCost;
    }
}
