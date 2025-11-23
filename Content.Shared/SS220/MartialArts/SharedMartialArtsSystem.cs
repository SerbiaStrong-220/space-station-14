// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.MartialArts;

public abstract partial class SharedMartialArtsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantMartialArtComponent, ActivateInWorldEvent>(OnActivateGrant);
    }

    private void OnActivateGrant(EntityUid uid, GrantMartialArtComponent comp, ActivateInWorldEvent ev)
    {
        if (ev.Handled)
            return;

        if (!TryComp<MartialArtistComponent>(ev.User, out var artist))
            return;

        if (TryGrantMartialArt(ev.User, comp.MartialArt, false, artist))
        {
            if (comp.DestroyAfterUse)
                QueueDel(uid);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("martial-arts-cant-grant"), ev.User);
        }

        ev.Handled = true;
    }

    public bool TryGrantMartialArt(EntityUid user, ProtoId<MartialArtPrototype> martialArt, bool overrideExisting = false, MartialArtistComponent? artist = null)
    {
        if (!Resolve(user, ref artist))
            return false;

        if (artist.MartialArt != null && !overrideExisting)
            return false;

        if (artist.MartialArt != null)
        {
            // TODO: shutdown effects
        }
        artist.MartialArt = martialArt;
        // TODO: setup effects

        return true;
    }

    /// <summary>
    /// Checks current combo for timeout and breaks it if combo timed out
    /// </summary>
    public void RefreshSequence(EntityUid user, MartialArtistComponent artist)
    {
        if (artist.CurrentSteps.Count > 0 && !CheckSequenceTimeout(artist))
        {
            Log.Info($"Sequence of user ({user}) timed out");
            ResetSequence(user, artist);
        }
    }

    /// <returns>true for valid sequence and false for timed out</returns>
    private bool CheckSequenceTimeout(MartialArtistComponent artist)
    {
        return artist.LastStepPerformedAt + artist.SequenceTimeout > _timing.CurTime;
    }

    public void PerformStep(EntityUid user, EntityUid target, CombatSequenceStep step, MartialArtistComponent? artist = null)
    {
        Log.Info("PerformStep called");
        if (!Resolve(user, ref artist))
            return;

        if (artist.MartialArt is not { } martialArt)
            return;

        Log.Info($"Performing step \"{step}\" for user ({user}) with target ({target})");
        RefreshSequence(user, artist);

        var sequences = GetSequences(martialArt);

        AddStep(user, artist, step);

        if (!TryGetSequence(artist.CurrentSteps, sequences, out var sequence, out var complete))
        {
            Log.Info($"Failed to get sequence for user ({user})");
            ResetSequence(user, artist);
            return;
        }

        if (complete)
        {
            Log.Info($"Sequence \"{Loc.GetString(sequence.Value.Name)}\" completed, performing checks and complition");

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

    private List<CombatSequence> GetSequences(ProtoId<MartialArtPrototype> martialArt)
    {
        if (!_prototype.TryIndex(martialArt, out var proto))
            return [];
        return proto.Sequences;
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
    }

    private void PerformSequenceEntry(EntityUid user, EntityUid target, MartialArtistComponent artist, CombatSequenceEntry entry, CombatSequence sequence)
    {
        // conditions
        foreach (var condition in entry.Conditions)
        {
            if (!condition.Execute(user, target, artist) ^ condition.Invert)
            {
                Log.Info($"Conditions check failed for \"{Loc.GetString(sequence.Name)}\"");
                ResetSequence(user, artist);
                return;
            }
        }

        // effects
        Log.Info($"Performing effects of sequence \"{Loc.GetString(sequence.Name)}\"");
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

    /// Called when sequence breaks, due to misstep, not met condition or just timeout
    private void ResetSequence(EntityUid user, MartialArtistComponent artist)
    {
        Log.Info($"Sequence reset for user ({user})");
        artist.CurrentSteps = [];
        artist.LastStepPerformedAt = TimeSpan.Zero;
        Dirty(user, artist);
    }

    private bool TryGetSequence(List<CombatSequenceStep> subsequence, List<CombatSequence> sequences, [NotNullWhen(true)] out CombatSequence? found, out bool complete)
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
}
