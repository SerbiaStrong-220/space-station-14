// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.MartialArts;

public abstract partial class SharedMartialArtsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MartialArtistComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<MartialArtistComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, MartialArtistComponent artist, ref ComponentGetState ev)
    {
        ev.State = new MartialArtistComponentState(artist.CurrentSteps, artist.LastStepPerformedAt);
    }

    private void OnHandleState(EntityUid uid, MartialArtistComponent artist, ref ComponentHandleState ev)
    {
        if (ev.Current is not MartialArtistComponentState state)
            return;

        artist.CurrentSteps = state.CurrentSteps;
        artist.LastStepPerformedAt = state.LastStepPerformedAt;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MartialArtistComponent>();

        while (query.MoveNext(out var user, out var artist))
        {
            artist.UpdateAccumulator += frameTime;

            if (artist.UpdateAccumulator > artist.UpdateRate)
            {
                artist.UpdateAccumulator = 0;
                RefreshSequence(user, artist);
            }
        }
    }

    /// <summary>
    /// Checks current combo for timeout and breaks it if combo timed out
    /// </summary>
    public void RefreshSequence(EntityUid user, MartialArtistComponent artist)
    {
        if (artist.CurrentSteps.Count > 0 && artist.LastStepPerformedAt.Add(artist.SequenceTimeout) < _timing.CurTime)
        {
            Log.Info($"Sequence of user ({user}) timed out");
            BreakSequence(user, artist);
        }
    }

    public void PerformStep(EntityUid user, EntityUid target, CombatSequenceStep step, MartialArtistComponent? artist = null)
    {
        Log.Info("PerformStep called");
        if (!Resolve(user, ref artist))
            return;

        if (artist.MartialArt is not { } martialArt)
            return;

        Log.Info($"Performing step \"{step}\" for user ({user}) with target ({target})");

        var sequences = GetSequences(martialArt);

        RefreshSequence(user, artist);

        AddStep(user, artist, step);

        if (!TryGetSequence(artist.CurrentSteps, sequences, out var sequence, out var complete))
        {
            Log.Info($"Failed to get sequence for user ({user})");
            BreakSequence(user, artist);
            return;
        }

        if (complete)
        {
            if (sequence.Value.Name != null)
            {
                Log.Info($"Sequence \"{sequence.Value.Name}\" completed, performing checks and complition");
            }

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
            return new();

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
        artist.CurrentSteps = new();
        Dirty(user, artist);

        PerformSequenceEntry(user, target, artist, sequence.Entry, sequence);
    }

    private void PerformSequenceEntry(EntityUid user, EntityUid target, MartialArtistComponent artist, CombatSequenceEntry entry, CombatSequence sequence)
    {
        // conditions
        foreach (var condition in entry.Conditions)
        {
            if (!condition.Execute(user, target, artist) ^ condition.Invert)
            {
                if (sequence.Name != null)
                {
                    Log.Info($"Conditions check failed for \"{sequence.Name}\"");
                }
                BreakSequence(user, artist);
                return;
            }
        }

        // effects
        if (sequence.Name != null)
        {
            Log.Info($"Performing effects of sequence \"{sequence.Name}\"");
        }
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
    private void BreakSequence(EntityUid user, MartialArtistComponent artist)
    {
        Log.Info($"Sequence broken for user ({user})");
        artist.CurrentSteps = new();
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
            }
        }

        return found != null;
    }

    private bool IsSubsequence(List<CombatSequenceStep> subsequence, List<CombatSequenceStep> sequence)
    {
        if (subsequence.Count == 0) return true;
        if (subsequence.Count > sequence.Count) return false;

        int i = 0, j = 0;

        while (i < subsequence.Count && j < sequence.Count)
        {
            if (subsequence[i] == sequence[j])
            {
                i++;
            }
            j++;
        }

        return i == subsequence.Count;
    }
}
