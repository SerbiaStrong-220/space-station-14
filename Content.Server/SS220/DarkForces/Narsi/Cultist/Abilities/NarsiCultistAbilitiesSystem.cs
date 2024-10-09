using Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities.Prototype;
using Content.Server.SS220.DarkForces.Narsi.Progress;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.SS220.DarkForces.Narsi.Abilities.Events;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities;

public sealed partial class NarsiCultistAbilitiesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly NarsiCultProgressSystem _progressSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    private const string EmpAction = "NarsiCultistEmp";
    private const string FireAction = "NarsiCultistFireArms";
    private const string StealthAction = "NarsiCultistStealth";
    private const string ShadowAction = "NarsiCultistShadow";
    private const string TeleportAction = "NarsiCultistTeleport";
    private const string SilenceAction = "NarsiCultistSilence";
    private const string BlindnessAction = "NarsiCultistBlindness";
    private const string StunAction = "NarsiCultistStun";
    private const string CuffAction = "NarsiCultistCuff";
    private const string GhostWeaponAction = "NarsiCultistGhostWeapon";

    public override void Initialize()
    {
        base.Initialize();

        InitializeEmp();
        InitializeSilence();
        InitializeStealth();
        InitializeBlindness();
        InitializeFireArms();
        InitializeShadow();
        InitializeStun();
        InitializeTeleport();
        InitializeCuff();
        InitializeGhostWeapon();
        InitializeLeaderAbilities();
    }

    private void OnCultistAbility(EntityUid uid, INarsiCultistAbility ability)
    {
        if (ability.Speech == null)
            return;

        _chatSystem.TrySendInGameICMessage(uid, ability.Speech, InGameICChatType.Whisper, false);
    }

    public void UpdateAbility(string id)
    {
        var level = _progressSystem.GetAbilityLevel(id);
        var cultists = EntityQueryEnumerator<NarsiCultistComponent>();
        while (cultists.MoveNext(out _, out var component))
        {
            if (!component.Abilities.TryGetValue(id, out var ability))
                continue;

            if (!_actionsSystem.TryGetActionData(ability, out var action))
                continue;

            action.UseDelay = level switch
            {
                1 => action.UseDelay,
                2 => action.UseDelay * 0.9,
                _ => action.UseDelay * 0.8
            };
        }
    }

    public bool TryLearnAbility(EntityUid uid, string id)
    {
        if (!TryComp<NarsiCultistComponent>(uid, out var cultistComponent))
            return false;

        if (cultistComponent.Abilities.ContainsKey(id))
            return false;

        var prototype = _prototypeManager.Index<NarsiAbilityPrototype>(id);

        EntityUid? actionUid = null;
        if (!_actionsSystem.AddAction(uid, ref actionUid, prototype.ActionId, uid))
            return false;

        cultistComponent.Abilities[id] = actionUid;

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateStealth();
        UpdateSilence();
        UpdateFireArms();
        UpdateBlindness();
    }
}
