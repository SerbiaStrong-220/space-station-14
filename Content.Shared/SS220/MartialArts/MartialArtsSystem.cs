// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.CombatMode;
using Content.Shared.Effects;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
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
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;

    private static readonly ProtoId<AlertPrototype> CooldownAlert = "MartialArtCooldown";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MartialArtistComponent, DisarmAttackPerformedEvent>(OnDisarm);
        SubscribeLocalEvent<MartialArtistComponent, LightAttackPerformedEvent>(OnHarm);
        SubscribeLocalEvent<MartialArtistComponent, PullStartedMessage>(OnGrab);

        SubscribeLocalEvent<MartialArtistComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<MartialArtOnTriggerComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<MartialArtOnEquipComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MartialArtOnEquipComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<MartialArtOnEquipComponent, ComponentShutdown>(OnEquipShutdown);
    }

    #region Public API

    public List<MartialArtEffect> GetMartialArtEffects(EntityUid user, MartialArtistComponent? artist = null)
    {
        if (!Resolve(user, ref artist))
            return [];

        if (!_prototype.TryIndex(artist.MartialArt, out var martialArt))
            return [];

        return martialArt.Effects.ToList();
    }

    /// <summary>
    /// Checks current combo for timeout and breaks it if combo timed out
    /// </summary>
    public void RefreshSequence(EntityUid user, MartialArtistComponent artist)
    {
        if (artist.CurrentSteps.Count > 0 && !CheckSequenceTimeout(artist))
        {
            ResetSequence(user, artist);
        }
    }

    public void PerformStep(EntityUid user, EntityUid target, CombatSequenceStep step, MartialArtistComponent? artist = null)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Resolve(user, ref artist))
            return;

        if (artist.MartialArt is not { } martialArt)
            return;

        if (!CanAttack(user, artist))
            return;

        RefreshSequence(user, artist);

        var sequences = GetSequences(martialArt);

        AddStep(user, artist, step);

        if (!TryGetSequence(artist.CurrentSteps, sequences, out var sequence, out var complete))
        {
            ResetSequence(user, artist);
            return;
        }

        if (complete)
        {
            PerformSequence(user, target, artist, sequence.Value);
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

    private bool CanAttack(EntityUid user, MartialArtistComponent? artist = null)
    {
        if (!Resolve(user, ref artist))
            return false;

        if (_melee.TryGetWeapon(user, out var meleeUid, out _) && meleeUid != user)
            return false;

        if (IsInCooldown(user, artist))
            return false;

        return true;
    }

    private void OnDisarm(EntityUid user, MartialArtistComponent artist, ref DisarmAttackPerformedEvent ev)
    {
        if (ev.Target is not { } target)
            return;

        if (artist.MartialArt == null)
            return;

        if (!CanBeAttackedWithMartialArts(target))
            return;

        PerformStep(user, target, CombatSequenceStep.Push, artist);
        _color.RaiseEffect(Color.Aqua, new List<EntityUid> { target }, Filter.Pvs(user, entityManager: EntityManager));
    }

    private void OnHarm(EntityUid user, MartialArtistComponent artist, ref LightAttackPerformedEvent ev)
    {
        if (ev.Target is not { } target)
            return;

        if (artist.MartialArt == null)
            return;

        if (!CanBeAttackedWithMartialArts(target))
            return;

        PerformStep(user, target, CombatSequenceStep.Harm, artist);
        _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(user, entityManager: EntityManager));
    }

    private void OnGrab(EntityUid user, MartialArtistComponent artist, ref PullStartedMessage ev)
    {
        // cuz this event is raised on both at the time
        if (user != ev.PullerUid)
            return;

        if (!_combatMode.IsInCombatMode(user))
            return;

        if (artist.MartialArt == null)
            return;

        if (!CanBeAttackedWithMartialArts(ev.PulledUid))
            return;

        PerformStep(user, ev.PulledUid, CombatSequenceStep.Grab, artist);
        _color.RaiseEffect(Color.Yellow, new List<EntityUid> { ev.PulledUid }, Filter.Pvs(user, entityManager: EntityManager));
    }

    /// <returns>true for valid sequence and false for timed out</returns>
    private bool CheckSequenceTimeout(MartialArtistComponent artist)
    {
        return artist.LastStepPerformedAt + artist.SequenceTimeout > _timing.CurTime;
    }

    public bool IsInCooldown(EntityUid user, MartialArtistComponent? artist = null)
    {
        if (!Resolve(user, ref artist))
            return false;

        return _timing.CurTime < artist.LastSequencePerformedAt + artist.LastSequenceCooldown;
    }

    private List<CombatSequence> GetSequences(ProtoId<MartialArtPrototype> martialArt)
    {
        if (!_prototype.TryIndex(martialArt, out var proto))
            return [];

        return proto.Sequences.ToList();
    }

    private void AddStep(EntityUid user, MartialArtistComponent artist, CombatSequenceStep step)
    {
        artist.CurrentSteps.Add(step);
        artist.LastStepPerformedAt = _timing.CurTime;
        Dirty(user, artist);
    }

    private void PerformSequence(EntityUid user, EntityUid target, MartialArtistComponent artist, CombatSequence sequence)
    {
        ResetSequence(user, artist);

        PerformSequenceEntry(user, target, artist, sequence.Entry, sequence);

        _popup.PopupClient(Loc.GetString("martial-arts-performed-sequence", ("sequence", Loc.GetString(sequence.Name))), user);

        artist.LastSequencePerformedAt = _timing.CurTime;
        artist.LastSequenceCooldown = sequence.Cooldown;

        _alerts.ShowAlert(user, CooldownAlert, null, (artist.LastSequencePerformedAt, artist.LastSequencePerformedAt + artist.LastSequenceCooldown), autoRemove: true);
    }

    private void PerformSequenceEntry(EntityUid user, EntityUid target, MartialArtistComponent artist, CombatSequenceEntry entry, CombatSequence sequence)
    {
        // conditions
        foreach (var condition in entry.Conditions)
        {
            if (!condition.Execute(user, target, artist) ^ condition.Invert)
            {
                ResetSequence(user, artist);
                return;
            }
        }

        // effects
        foreach (var effect in entry.Effects)
        {
            effect.Execute(user, target, artist);
        }

        // recursive entries
        foreach (var subentry in entry.Entries)
        {
            PerformSequenceEntry(user, target, artist, subentry, sequence);
        }
    }

    /// <summary>
    /// Called to clear current sequence state
    /// </summary>
    private void ResetSequence(EntityUid user, MartialArtistComponent artist)
    {
        artist.CurrentSteps = [];
        artist.LastStepPerformedAt = TimeSpan.Zero;
        Dirty(user, artist);
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
