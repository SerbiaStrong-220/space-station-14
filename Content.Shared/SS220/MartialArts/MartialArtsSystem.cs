// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Effects;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.Grab;
using Content.Shared.SS220.MartialArts.Effects;
using Content.Shared.Trigger;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.MartialArts;

public sealed partial class MartialArtsSystem : EntitySystem, IMartialArtEffectEventRaiser
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedGrabSystem _grab = default!;

    private static readonly ProtoId<AlertPrototype> CooldownAlert = "MartialArtCooldown";
    private static readonly LocId UnknownArt = "martial-arts-unknown";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MartialArtistComponent, DisarmAttackPerformedEvent>(OnDisarm);
        SubscribeLocalEvent<MartialArtistComponent, LightAttackPerformedEvent>(OnHarm);
        SubscribeLocalEvent<MartialArtistComponent, GrabStageChangeEvent>(OnGrab);

        SubscribeLocalEvent<MartialArtistComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<MartialArtOnTriggerComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<MartialArtOnEquipComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MartialArtOnEquipComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<MartialArtOnEquipComponent, ComponentShutdown>(OnEquipShutdown);

        SubscribeLocalEvent<MartialArtistComponent, GrabDelayModifiersEvent>(OnGrabDelayModifiers);
    }

    #region Public API

    public List<MartialArtEffect> GetMartialArtEffects(Entity<MartialArtistComponent?> user)
    {
        if (!Resolve(user.Owner, ref user.Comp))
            return [];

        if (!_prototype.TryIndex(user.Comp.MartialArt, out var martialArt))
            return [];

        return martialArt.Effects.ToList();
    }

    /// <summary>
    /// Checks current combo for timeout and breaks it if combo timed out
    /// </summary>
    public void RefreshSequence(Entity<MartialArtistComponent> user)
    {
        if (user.Comp.CurrentSteps.Count > 0 && !CheckSequenceTimeout(user.Comp))
        {
            ResetSequence(user);
        }
    }

    public void PerformStep(Entity<MartialArtistComponent?> user, EntityUid target, CombatSequenceStep step)
    {
        if (!Resolve(user.Owner, ref user.Comp))
            return;

        if (user.Comp.MartialArt is not { } martialArt)
            return;

        if (!CanAttack(user))
            return;

        RefreshSequence(user!);

        var sequences = GetSequences(martialArt);

        AddStep(user!, step);

        if (!TryGetSequence(user.Comp.CurrentSteps, sequences, out var sequence, out var complete))
        {
            ResetSequence(user!);
            return;
        }

        if (complete)
        {
            PerformSequence(user!, target, sequence.Value);
        }
    }

    public bool CanBeAttackedWithMartialArts(EntityUid target)
    {
        return HasComp<MartialArtsTargetComponent>(target);
    }

    public List<CombatSequenceStep> GetPerformedSteps(EntityUid artist)
    {
        if (!TryComp<MartialArtistComponent>(artist, out var comp))
            return [];

        if (!CheckSequenceTimeout(comp))
            return [];

        return comp.CurrentSteps;
    }

    #endregion

    #region Private API

    private bool CanAttack(Entity<MartialArtistComponent?> user)
    {
        if (!Resolve(user.Owner, ref user.Comp))
            return false;

        if (_melee.TryGetWeapon(user, out var meleeUid, out _) && meleeUid != user.Owner)
            return false;

        if (IsInCooldown(user))
            return false;

        return true;
    }

    private void OnDisarm(Entity<MartialArtistComponent> user, ref DisarmAttackPerformedEvent ev)
    {
        if (ev.Target is not { } target)
            return;

        if (user.Comp.MartialArt == null)
            return;

        if (!CanBeAttackedWithMartialArts(target))
            return;

        PerformStep(user!, target, CombatSequenceStep.Push);
        _color.RaiseEffect(Color.Aqua, new List<EntityUid> { target }, Filter.Pvs(user, entityManager: EntityManager));
    }

    private void OnHarm(Entity<MartialArtistComponent> user, ref LightAttackPerformedEvent ev)
    {
        if (ev.Target is not { } target)
            return;

        if (user.Comp.MartialArt == null)
            return;

        if (!CanBeAttackedWithMartialArts(target))
            return;

        PerformStep(user!, target, CombatSequenceStep.Harm);
        _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(user, entityManager: EntityManager));
    }

    private void OnGrab(Entity<MartialArtistComponent> user, ref GrabStageChangeEvent ev)
    {
        if (ev.Grabber != user.Owner)
            return;

        if (user.Comp.MartialArt == null)
            return;

        if (ev.NewStage == GrabStage.None)
        {
            if (user.Comp.CurrentSteps.Count > 0)
                ResetSequence(user);

            return;
        }

        if (!CanBeAttackedWithMartialArts(ev.Grabbable))
            return;

        PerformStep(user!, ev.Grabbable, CombatSequenceStep.Grab);
        _color.RaiseEffect(Color.Yellow, new List<EntityUid> { ev.Grabbable }, Filter.Pvs(user, entityManager: EntityManager));
    }

    private void OnGrabDelayModifiers(Entity<MartialArtistComponent> user, ref GrabDelayModifiersEvent ev)
    {
        if (!_prototype.TryIndex(user.Comp.MartialArt, out var martialArt))
            return;

        if (_grab.IsGrabbed(ev.Grabbable) && user.Comp.CurrentSteps.Count > 0)
        {
            ev.Multiply(0); // make it instant if target already grabbed and part of combo
            return;
        }

        ev.Multiply(martialArt.GrabDelayCoefficient);
    }

    /// <returns>true for valid sequence and false for timed out</returns>
    private bool CheckSequenceTimeout(MartialArtistComponent artist)
    {
        return artist.LastStepPerformedAt + artist.SequenceTimeout > _timing.CurTime;
    }

    public bool IsInCooldown(Entity<MartialArtistComponent?> user)
    {
        if (!Resolve(user.Owner, ref user.Comp))
            return false;

        return _timing.CurTime < user.Comp.LastSequencePerformedAt + user.Comp.LastSequenceCooldown;
    }

    private List<CombatSequence> GetSequences(ProtoId<MartialArtPrototype> martialArt)
    {
        if (!_prototype.TryIndex(martialArt, out var proto))
            return [];

        return proto.Sequences.ToList();
    }

    private void AddStep(Entity<MartialArtistComponent> user, CombatSequenceStep step)
    {
        user.Comp.CurrentSteps.Add(step);
        user.Comp.LastStepPerformedAt = _timing.CurTime;
        Dirty(user);
    }

    private void PerformSequence(Entity<MartialArtistComponent> user, EntityUid target, CombatSequence sequence)
    {
        ResetSequence(user);

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable);

        if (_grab.IsGrabbed(target) && !sequence.PreventGrabReset)
            _grab.BreakGrab(target);

        PerformSequenceEntry(user, target, sequence.Entry, sequence);

        _popup.PopupClient(Loc.GetString(user.Comp.PerformedSequencePopup, ("sequence", Loc.GetString(sequence.Name))), user);

        user.Comp.LastSequencePerformedAt = _timing.CurTime;
        user.Comp.LastSequenceCooldown = sequence.Cooldown;

        _alerts.ShowAlert(user.Owner, CooldownAlert, null, (user.Comp.LastSequencePerformedAt, user.Comp.LastSequencePerformedAt + user.Comp.LastSequenceCooldown), autoRemove: true);
    }

    private void PerformSequenceEntry(Entity<MartialArtistComponent> user, EntityUid target, CombatSequenceEntry entry, CombatSequence sequence)
    {
        // conditions
        foreach (var condition in entry.Conditions)
        {
            if (!condition.Execute(user, target) ^ condition.Invert)
            {
                ResetSequence(user);
                return;
            }
        }

        // effects
        foreach (var effect in entry.Effects)
        {
            effect.Execute(user, target);
        }

        // recursive entries
        foreach (var subentry in entry.Entries)
        {
            PerformSequenceEntry(user, target, subentry, sequence);
        }
    }

    /// <summary>
    /// Called to clear current sequence state
    /// </summary>
    private void ResetSequence(Entity<MartialArtistComponent> user)
    {
        user.Comp.CurrentSteps = [];
        user.Comp.LastStepPerformedAt = TimeSpan.Zero;
        Dirty(user);
    }

    // made public for tests
    public bool TryGetSequence(List<CombatSequenceStep> subsequence, List<CombatSequence> sequences, [NotNullWhen(true)] out CombatSequence? found, out bool complete)
    {
        found = null;
        complete = false;

        foreach (var sequence in sequences)
        {
            if (IsSubsequence(subsequence, sequence.Steps))
            {
                found = sequence;

                if (subsequence.Count == sequence.Steps.Count)
                    complete = true;
                return true;
            }
        }

        return false;
    }

    private bool IsSubsequence(List<CombatSequenceStep> subsequence, List<CombatSequenceStep> sequence)
    {
        if (subsequence.Count == 0)
            return true;

        if (subsequence.Count > sequence.Count)
            return false;

        var subIndex = 0;

        foreach (var step in sequence)
        {
            if (step == subsequence[subIndex])
            {
                subIndex++;

                if (subIndex == subsequence.Count)
                    return true;
            }
        }

        return false;
    }

    #endregion
}
