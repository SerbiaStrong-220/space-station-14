// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Effects;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.SS220.MartialArts;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Server.SS220.MartialArts;

public sealed partial class MartialArtsSystem : SharedMartialArtsSystem
{
    [Dependency] private readonly ColorFlashEffectSystem _color = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MartialArtistComponent, DisarmAttackPerformedEvent>(OnDisarm);
        SubscribeLocalEvent<MartialArtistComponent, LightAttackPerformedEvent>(OnHarm);
        SubscribeLocalEvent<MartialArtistComponent, PullStartedMessage>(OnGrab);
    }

    public void OnDisarm(EntityUid user, MartialArtistComponent artist, ref DisarmAttackPerformedEvent ev)
    {
        if (ev.Target is not { } target)
            return;

        if (artist.MartialArt == null)
            return;

        if (!CanBeAttackedWithMartialArts(target))
            return;

        PerformStep(user, target, CombatSequenceStep.Push, artist);
        _color.RaiseEffect(Color.Aqua, new List<EntityUid>() { target }, Filter.Pvs(user, entityManager: EntityManager));
    }

    public void OnHarm(EntityUid user, MartialArtistComponent artist, ref LightAttackPerformedEvent ev)
    {
        Log.Info("OnHarm raised");
        if (ev.Target is not { } target)
            return;

        if (artist.MartialArt == null)
            return;

        if (!CanBeAttackedWithMartialArts(target))
            return;

        Log.Info("OnHarm checks passed, performing");
        PerformStep(user, target, CombatSequenceStep.Harm, artist);
        _color.RaiseEffect(Color.Red, new List<EntityUid>() { target }, Filter.Pvs(user, entityManager: EntityManager));
    }

    public void OnGrab(EntityUid user, MartialArtistComponent artist, ref PullStartedMessage ev)
    {
        // cuz this event is raised on both at the time
        if (user != ev.PullerUid)
            return;

        if (artist.MartialArt == null)
            return;

        if (!CanBeAttackedWithMartialArts(ev.PulledUid))
            return;

        PerformStep(user, ev.PulledUid, CombatSequenceStep.Grab, artist);
        _color.RaiseEffect(Color.Yellow, new List<EntityUid>() { ev.PulledUid }, Filter.Pvs(user, entityManager: EntityManager));
    }
}
