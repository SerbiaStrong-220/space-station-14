// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Content.Server.Atmos.Rotting;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Electrocution;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Server.Traits.Assorted;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.PowerCell;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Text.Json.Serialization;

using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CultYogg;

public sealed class MiGoHealSystem : SharedMiGoHealSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chatManager = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    /// <summary>
    /// Damage to apply every metabolism cycle. Damage Ignores resistances.
    /// </summary>
    [DataField(required: true)]
    [JsonPropertyName("damage")]
    public DamageSpecifier Damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoHealComponent, ComponentStartup>(SetupMiGoHeal);
    }
    private void SetupMiGoHeal(Entity<MiGoHealComponent> uid, ref ComponentStartup args)
    {
        uid.Comp.NextIncidentTime = uid.Comp.TimeBetweenIncidents;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MiGoHealComponent>();
        while (query.MoveNext(out var uid, out var healing))
        {
            healing.NextIncidentTime -= frameTime;

            if (healing.NextIncidentTime >= 0)
                continue;

            Heal(uid, healing);

            //if dmg<100
            //Revive(uid, healing);

            healing.NextIncidentTime += healing.TimeBetweenIncidents;

        }
    }
    public void Heal(EntityUid uid, MiGoHealComponent component)
    {
        _entityManager.System<DamageableSystem>().TryChangeDamage(uid, component.Damage, true, interruptsDoAfters: false);
    }

    public void Revive(EntityUid uid, MiGoHealComponent component)
    {
        //Revive dead -- copypaste from Zap DefibrillatorSystem
        /*
        if(TryGetComponent(uid,))

        if (_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold) &&
            TryComp<DamageableComponent>(uid, out var damageableComponent) &&
            damageableComponent.TotalDamage < threshold)
        {
            _mobState.ChangeMobState(uid, MobState.Critical, mob, uid);
            dead = false;
        }

        if (_mind.TryGetMind(uid, out _, out var mind) &&
            mind.Session is { } playerSession)
        {
            session = playerSession;
            // notify them they're being revived.
            if (mind.CurrentEntity != uid)
            {
                _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind), session);
            }
        }
        */

        // Set the new time.
    }
}
