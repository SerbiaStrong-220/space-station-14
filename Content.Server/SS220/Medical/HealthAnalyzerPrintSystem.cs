using System.Linq;
using System.Text;
using Content.Server.Medical.Components;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.SS220.MedicalScanner;
using Content.Shared.SS220.Paper;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Medical;

public sealed class HealthAnalyzerPrintSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedDocumentHelperSystem _documentHelper = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!; // SS220-health-analyzer-report
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerPrintMessage>(OnPrint);
    }

    private void OnPrint(Entity<HealthAnalyzerComponent> ent, ref HealthAnalyzerPrintMessage args)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;
        var user = args.Actor;

        if (!comp.CanPrint)
            return;

        if (_timing.CurTime < comp.PrintReadyAt)
        {
            _popupSystem.PopupEntity(Loc.GetString("health-analyzer-printer-not-ready"), uid, user);
            return;
        }

        if (string.IsNullOrWhiteSpace(comp.LastScannedReport))
        {
            _popupSystem.PopupEntity(Loc.GetString("health-analyzer-printer-no-data"), uid, user);
            return;
        }

        var printed = Spawn(comp.MachineOutput, Transform(uid).Coordinates);
        _handsSystem.PickupOrDrop(user, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
        {
            Log.Error("Printed health analyzer report did not have PaperComponent.");
            return;
        }

        _metaData.SetEntityName(printed, Loc.GetString("health-analyzer-report-title", ("entity", comp.LastScannedName)));
        _paperSystem.SetContent((printed, paperComp), comp.LastScannedReport);
        _audio.PlayPvs(comp.SoundPrint, uid);

        comp.PrintReadyAt = _timing.CurTime + comp.PrintCooldown;
    }

    public string BuildScanReport(
        EntityUid target,
        DamageableComponent damageable,
        string scannedName,
        float bodyTemperature,
        float bloodAmount,
        bool? bleeding,
        bool? unrevivable,
        int? counterDeath)
    {
        var builder = new StringBuilder();

        builder.AppendLine(Loc.GetString("health-analyzer-report-section-patient"));
        builder.AppendLine(Loc.GetString("health-analyzer-report-name", ("name", scannedName)));

        var dateTime = $"{_documentHelper.GetGameDate()} {_documentHelper.GetStationTime()}";
        builder.AppendLine(Loc.GetString("health-analyzer-report-date-time", ("dateTime", dateTime)));

        var species = TryComp<HumanoidProfileComponent>(target, out var humanoidAppearance)
            ? Loc.GetString(_prototypeManager.Index(humanoidAppearance.Species).Name)
            : Loc.GetString("health-analyzer-window-entity-unknown-species-text");
        builder.AppendLine(Loc.GetString("health-analyzer-report-species", ("species", species)));

        var status = TryComp<MobStateComponent>(target, out var mobState)
            ? GetStatus(mobState.CurrentState)
            : Loc.GetString("health-analyzer-window-entity-unknown-text");
        builder.AppendLine(Loc.GetString("health-analyzer-report-status", ("value", status)));

        var temperature = !float.IsNaN(bodyTemperature)
            ? $"{bodyTemperature - Atmospherics.T0C:F1} °C ({bodyTemperature:F1} K)"
            : Loc.GetString("health-analyzer-window-entity-unknown-value-text");
        builder.AppendLine(Loc.GetString("health-analyzer-report-temperature", ("value", temperature)));

        var blood = !float.IsNaN(bloodAmount)
            ? $"{bloodAmount * 100:F1} %"
            : Loc.GetString("health-analyzer-window-entity-unknown-value-text");
        builder.AppendLine(Loc.GetString("health-analyzer-report-blood-level", ("value", blood)));

        var deathCount = counterDeath?.ToString() ?? Loc.GetString("health-analyzer-window-entity-unknown-value-text");
        var allDamage = _damageable.GetAllDamage(target);

        builder.AppendLine(Loc.GetString("health-analyzer-report-death-counter", ("value", deathCount)));
        builder.AppendLine(Loc.GetString("health-analyzer-report-total-damage", ("value", allDamage.GetTotal())));
        builder.AppendLine();

        builder.AppendLine(Loc.GetString("health-analyzer-report-section-alerts"));
        if (unrevivable == true)
            builder.AppendLine($"- {Loc.GetString("health-analyzer-report-entity-unrevivable-text")}");
        if (bleeding == true)
            builder.AppendLine($"- {Loc.GetString("health-analyzer-report-entity-bleeding-text")}");
        if (unrevivable != true && bleeding != true)
            builder.AppendLine(Loc.GetString("health-analyzer-report-none"));
        builder.AppendLine();

        builder.AppendLine(Loc.GetString("health-analyzer-report-section-damage"));
        var damageGroups = allDamage.GetDamagePerGroup(_prototypeManager).OrderByDescending(damage => damage.Value).ToList();
        var hasDamageGroups = false;
        foreach (var (damageGroupId, damageAmount) in damageGroups)
        {
            if (damageAmount == 0)
                continue;

            hasDamageGroups = true;
            builder.AppendLine(Loc.GetString(
                "health-analyzer-window-damage-group-text",
                ("damageGroup", _prototypeManager.Index(damageGroupId).LocalizedName),
                ("amount", damageAmount)));

            var group = _prototypeManager.Index(damageGroupId);
            foreach (var type in group.DamageTypes)
            {
                if (!allDamage.DamageDict.TryGetValue(type, out var typeAmount) || typeAmount <= 0)
                    continue;

                builder.AppendLine($" · {Loc.GetString(
                    "health-analyzer-window-damage-type-text",
                    ("damageType", _prototypeManager.Index<DamageTypePrototype>(type).LocalizedName),
                    ("amount", typeAmount))}");
            }
        }

        if (!hasDamageGroups)
            builder.AppendLine(Loc.GetString("health-analyzer-report-none"));

        builder.AppendLine();
        builder.AppendLine(Loc.GetString("health-analyzer-report-section-reagents"));

        var hasReagents = false;
        if (TryComp<SolutionContainerManagerComponent>(target, out var solComp) &&
            _solutionContainerSystem.TryGetSolution((target, solComp), BloodstreamComponent.DefaultBloodSolutionName, out var solution))
        {
            foreach (var (reagent, valueReagent) in solution.Value.Comp.Solution.Contents)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Prototype, out var reagentPrototype))
                    continue;

                hasReagents = true;
                builder.AppendLine(Loc.GetString(
                    "health-analyzer-window-entity-reagent-text",
                    ("reagentName", reagentPrototype.LocalizedName),
                    ("reagentValue", valueReagent)));
            }
        }

        if (!hasReagents)
            builder.AppendLine(Loc.GetString("health-analyzer-report-none"));

        return builder.ToString();
    }

    private string GetStatus(MobState mobState)
    {
        return mobState switch
        {
            MobState.Alive => Loc.GetString("health-analyzer-window-entity-alive-text"),
            MobState.Critical => Loc.GetString("health-analyzer-window-entity-critical-text"),
            MobState.Dead => Loc.GetString("health-analyzer-window-entity-dead-text"),
            _ => Loc.GetString("health-analyzer-window-entity-unknown-text"),
        };
    }
}
