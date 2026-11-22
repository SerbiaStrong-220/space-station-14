// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion;
using Content.Shared.FixedPoint;
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
    [Dependency] private DamageableSystem _damageable = default!;
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

    // a hit: (damage - offset) * per damage, clamped
    private const float HitChanceOffset = 9.17f;
    private const float HitChancePerDamage = 0.06f;
    private const float HitMinChance = 0.20f;
    private const float HitMaxChance = 0.95f;

    // an explosion, by its blunt damage after armor
    private const float BlastThreshold = 20f;
    private const float BlastChanceOffset = 10f;
    private const float BlastChancePerDamage = 0.005f;
    private const float BlastMinChance = 0.05f;
    private const float BlastMaxChance = 0.40f;

    private const float TearSlashDamage = 20f;
    private const float TearThrowMinDistance = 2f;
    private const float TearThrowMaxDistance = 3f;

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

        var damage = (GetDamageAmount(delta, DamageBlunt) + GetDamageAmount(delta, DamageSlash)).Float();
        if (!_prototype.Resolve(ent.Comp.HitZones, out var zones))
            return;

        ProtoId<OrganCategoryPrototype> category = zones.Pick(_random);
        if (!_body.TryGetOrganByCategory(ent.Owner, category, out var organ)
            || !TryComp<DismemberableComponent>(organ, out var dismemberable)
            || damage < dismemberable.HitThreshold
            || !HasSevereTrauma(ent, category))
            return;

        var chance = dismemberable.HitChance
            ?? Math.Clamp((damage - HitChanceOffset) * HitChancePerDamage, HitMinChance, HitMaxChance);

        if (_random.Prob(chance))
            TearOff(ent, organ.Value);
    }

    private bool HasSevereTrauma(EntityUid body, ProtoId<OrganCategoryPrototype> category)
    {
        if (!TryComp<PathologyHolderComponent>(body, out var holder))
            return false;

        foreach (var pathologyId in holder.ActivePathologies.Keys)
        {
            if (_prototype.Resolve(pathologyId, out var pathology) && pathology.IsSevere && pathology.Organ == category)
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
        var blunt = GetDamageAmount(args.Damage, DamageBlunt).Float();
        if (blunt < BlastThreshold || !_body.TryGetOrgansWithComponent<DismemberableComponent>(ent.Owner, out var parts))
            return;

        var chance = Math.Clamp((blunt - BlastChanceOffset) * BlastChancePerDamage, BlastMinChance, BlastMaxChance);
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

        var direction = _random.NextAngle().ToVec() * _random.NextFloat(TearThrowMinDistance, TearThrowMaxDistance);
        _throwing.TryThrow(part, direction, playSound: false);

        _popup.PopupEntity(Loc.GetString("limb-dismembered-popup", ("target", Identity.Entity(ent, EntityManager)), ("part", part)),
            ent, PopupType.LargeCaution);
        _audio.PlayPvs(TearSound, ent);

        // the emote ignores the action blocker, so it would come even from a corpse
        if (_mobState.IsAlive(ent))
            _chat.TryEmoteWithChat(ent, ScreamEmote, ChatTransmitRange.Normal, ignoreActionBlocker: true);

        if (TryComp<BloodstreamComponent>(ent, out var bloodstream))
            _bloodstream.TryModifyBleedAmount((ent, bloodstream), bloodstream.MaxBleedAmount);

        // the wound damage must not roll another tear in the same tick
        ent.Comp.TearTick = _timing.CurTick;
        _damageable.TryChangeDamage(ent.Owner, new DamageSpecifier(_prototype.Index(DamageSlash), TearSlashDamage), ignoreResistances: true);
    }

    private static FixedPoint2 GetDamageAmount(DamageSpecifier damage, ProtoId<DamageTypePrototype> type)
    {
        return damage.DamageDict.TryGetValue(type, out var amount) && amount > FixedPoint2.Zero ? amount : FixedPoint2.Zero;
    }
}
