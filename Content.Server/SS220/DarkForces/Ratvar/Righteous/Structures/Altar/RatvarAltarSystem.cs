using System.Linq;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress;
using Content.Server.Mind;
using Content.Shared.Buckle.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities.Structures;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Server.Antag;
using Robust.Shared.Player;
using Content.Shared.SS220.CCVars;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Structures.Altar;

public sealed class RatvarAltarSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPointLightSystem _lightningSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly RatvarProgressSystem _progressSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _sharedAppearance = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string AltarGlow = "RatvarAltarActivateEffect";

    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultRatvarRule = "Ratvar";

    private readonly TimeSpan _timeToConvert = TimeSpan.FromSeconds(8);
    private readonly TimeSpan _timeToDie = TimeSpan.FromSeconds(16);

    private const int PowerForConvert = 500;
    private const int PowerForDie = 300;

    private int _maxRighteousCount = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RatvarAltarComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<RatvarAltarComponent, UnstrappedEvent>(OnUnStrapped);
        SubscribeLocalEvent<RatvarAltarComponent, AnchorStateChangedEvent>(OnAnchorChanged);

        _cfg.OnValueChanged(CCVars220.RatvarMaxRighteousCount, OnMaxRighteousCountChanged);
        _maxRighteousCount = _cfg.GetCVar(CCVars220.RatvarMaxRighteousCount);
    }

    private void OnMaxRighteousCountChanged(int count)
    {
        _maxRighteousCount = count;
    }

    private void OnAnchorChanged(EntityUid uid, RatvarAltarComponent component, AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            UpdateBuckle((uid, component));
        else
        {
            component.Type = AltarActiveType.Idle;
            _sharedAppearance.SetData(uid, RatvarAltarVisuals.State, RatvarAltarState.UnAnchored);
            _lightningSystem.SetEnabled(uid, false);
        }
    }

    public void ForceMakeRatvarRighteous(ICommonSession session)
    {
        // TODO: Switch to MakeAntag
        _antagSelection.ForceMakeAntag<RatvarRuleComponent>(session, DefaultRatvarRule);
    }

    public void ForceMakeRatvarRighteous(EntityUid uid)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        ForceMakeRatvarRighteous(actor.PlayerSession);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<RatvarAltarComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var target = component.BuckledEntity;
            if (target == EntityUid.Invalid || component.Type == AltarActiveType.Idle)
                continue;

            if (component.ActivateTime > curTime)
                continue;

            switch (component.Type)
            {
                case AltarActiveType.Convert:
                    ForceMakeRatvarRighteous(target);
                    _progressSystem.TryRequestChangePower(PowerForConvert);
                    ToIdleState((uid, component));
                    break;
                case AltarActiveType.Die:
                    OnUserDie(target, component);
                    ToIdleState((uid, component));
                    _progressSystem.TryRequestChangePower(PowerForDie);
                    break;
            }
        }
    }

    private void OnUserDie(EntityUid target, RatvarAltarComponent component)
    {
        if (target == EntityUid.Invalid)
            return;

        if (_mindSystem.TryGetMind(target, out var mindId, out _))
        {
            var transform = Transform(target);
            var soulVessel = Spawn(component.SoulVessel, transform.Coordinates);
            _mindSystem.TransferTo(mindId, soulVessel);
        }

        QueueDel(target);
    }

    private void ToIdleState(Entity<RatvarAltarComponent> ent)
    {
        ent.Comp.Type = AltarActiveType.Idle;
        _sharedAppearance.SetData(ent, RatvarAltarVisuals.State, RatvarAltarState.AnchoredIdle);
        _lightningSystem.SetEnabled(ent, false);
    }

    private void OnStrapped(Entity<RatvarAltarComponent> ent, ref StrappedEvent args)
    {
        if (!args.Buckle.Comp.Buckled)
            return;

        ent.Comp.BuckledEntity = args.Buckle.Owner;
        UpdateBuckle(ent);
    }

    private void OnUnStrapped(Entity<RatvarAltarComponent> ent, ref UnstrappedEvent args)
    {
        ent.Comp.BuckledEntity = EntityUid.Invalid;
        UpdateBuckle(ent);
    }

    private void UpdateBuckle(Entity<RatvarAltarComponent> ent)
    {
        var target = ent.Comp.BuckledEntity;
        if (target == EntityUid.Invalid || HasComp<RatvarRighteousComponent>(target))
        {
            ToIdleState(ent);
            return;
        }

        if (UserValidToConvert(target))
        {
            ent.Comp.ActivateTime = _gameTiming.CurTime + _timeToConvert;
            ent.Comp.Type = AltarActiveType.Convert;
        }
        else if (UserValidToDie(target))
        {
            ent.Comp.ActivateTime = _gameTiming.CurTime + _timeToDie;
            ent.Comp.Type = AltarActiveType.Die;
        }
        else
            return;

        _sharedAppearance.SetData(ent, RatvarAltarVisuals.State, RatvarAltarState.Working);
        _lightningSystem.SetEnabled(ent, true);

        var transform = Transform(ent);
        Spawn(AltarGlow, transform.Coordinates);
    }

    private bool UserValidToConvert(EntityUid user)
    {
        if (!IsRighteousCountValid())
            return false;

        if (!HasComp<HumanoidAppearanceComponent>(user))
            return false;

        if (HasComp<MindShieldComponent>(user))
            return false;

        if (!_mindSystem.TryGetMind(user, out _, out _))
            return false;

        if (_mobStateSystem.IsDead(user))
            return false;

        return true;
    }

    private bool IsRighteousCountValid()
    {
        if (_maxRighteousCount == -1)
            return true;

        var query = EntityQuery<RatvarRighteousComponent>();
        return query.Count() < _maxRighteousCount;
    }

    private bool UserValidToDie(EntityUid user)
    {
        return HasComp<HumanoidAppearanceComponent>(user);
    }
}
