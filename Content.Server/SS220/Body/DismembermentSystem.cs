// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Body;
using Content.Shared.SS220.Pathology;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Body;

/// <summary>
/// Tears off body parts: a strong cut or blow may sever a part that has a severe trauma, explosions tear off parts marked for it.
/// </summary>
public sealed partial class DismembermentSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<DamageTypePrototype> DamageBlunt = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> DamageSlash = "Slash";
    private static readonly ProtoId<EmotePrototype> ScreamEmote = "Scream";
    private static readonly SoundSpecifier TearSound = new SoundCollectionSpecifier("gib");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DismembermentComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<DismembermentComponent, BeforeExplodeEvent>(OnExploded);
    }

    /// <summary>
    /// Only cuts and blows tear parts off, the hit lands on a random zone and needs a severe trauma there.
    /// </summary>
    private void OnDamageChanged(Entity<DismembermentComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta is not { } delta || ent.Comp.TearTick == _timing.CurTick || HasComp<GodmodeComponent>(ent))
            return;

        if (TryComp<PathologyHolderComponent>(ent, out var holder) && holder.DamageWithoutTraumas)
            return;

        var damage = (SharedPathologySystem.GetDealtDamage(delta, DamageBlunt) + SharedPathologySystem.GetDealtDamage(delta, DamageSlash)).Float();
        if (!_prototype.Resolve(ent.Comp.HitZones, out var zones))
            return;

        ProtoId<OrganCategoryPrototype> category = zones.Pick(_random);
        if (!_body.TryGetOrganByCategory(ent.Owner, category, out var organ)
            || !TryComp<DismemberableComponent>(organ, out var dismemberable)
            || (dismemberable.HitRoll ?? ent.Comp.HitRoll) is not { } hitRoll
            || !hitRoll.TryGetChance(damage, out var chance)
            || !HasSevereTrauma(ent, category))
            return;

        if (_random.Prob(chance))
            TearOff(ent, organ.Value);
    }

    private bool HasSevereTrauma(EntityUid body, ProtoId<OrganCategoryPrototype> category)
    {
        if (!TryComp<PathologyHolderComponent>(body, out var holder))
            return false;

        foreach (var (pathologyId, data) in holder.ActivePathologies)
        {
            if (_prototype.Resolve(pathologyId, out var pathology) && pathology.Organ == category && SharedPathologySystem.IsSevere(pathology, data))
                return true;
        }

        return false;
    }

    private void OnExploded(Entity<DismembermentComponent> ent, ref BeforeExplodeEvent args)
    {
        if (HasComp<GodmodeComponent>(ent))
            return;

        // the blast damage comes right after this event, it must not roll a hit too
        ent.Comp.TearTick = _timing.CurTick;

        // the damage is already reduced by explosion armor
        var blunt = SharedPathologySystem.GetDealtDamage(args.Damage, DamageBlunt).Float();
        if (ent.Comp.BlastRoll is not { } blastRoll
            || !blastRoll.TryGetChance(blunt, out var chance)
            || !_body.TryGetOrgansWithComponent<DismemberableComponent>(ent.Owner, out var parts))
            return;

        foreach (var part in parts)
        {
            if (!part.Comp.TornByBlast)
                continue;

            var modifier = TryComp<ProsthesisComponent>(part, out var prosthesis) ? prosthesis.ExplosionDismemberModifier : 1f;
            if (_random.Prob(Math.Clamp(chance * modifier, 0f, 1f)))
                TearOff(ent, part);
        }
    }

    private void TearOff(Entity<DismembermentComponent> ent, EntityUid part)
    {
        if (!TryComp<OrganComponent>(part, out var organ)
            || organ.Category is not { } category
            || !_body.TryExtractOrgan(ent.Owner, category, out _))
            return;

        var direction = _random.NextAngle().ToVec() * _random.NextFloat(ent.Comp.TearThrowMinDistance, ent.Comp.TearThrowMaxDistance);
        _throwing.TryThrow(part, direction, playSound: false);

        _popup.PopupEntity(Loc.GetString("limb-dismembered-popup", ("target", Identity.Entity(ent, EntityManager)), ("part", part)),
            ent, PopupType.LargeCaution);
        _audio.PlayPvs(TearSound, ent);

        // the emote ignores the action blocker, so it would come even from a corpse
        if (_mobState.IsAlive(ent))
            _chat.TryEmoteWithChat(ent, ScreamEmote, ChatTransmitRange.Normal, ignoreActionBlocker: true);

        if (TryComp<BloodstreamComponent>(ent, out var bloodstream))
            _bloodstream.TryModifyBleedAmount((ent, bloodstream), bloodstream.MaxBleedAmount);

        if (ent.Comp.TearDamage is not { } tearDamage)
            return;

        // the wound of the tear is not a hit, it must not roll traumas or another tear
        _pathology.TryChangeDamageWithoutTraumas(ent.Owner, tearDamage, out _, ignoreResistances: true);
    }
}
