// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Server.Wires;
using Content.Shared.Administration.Logs;
using Content.Shared.APC;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.MalfAI;
using Content.Shared.SS220.Power;
using Content.Shared.SS220.Power.Components;
using Content.Shared.Stacks;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Wires;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Power;

public sealed partial class PowerMalfunctionSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedToolSystem _tools = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;

    private static readonly ProtoId<ToolQualityPrototype> RepairQuality = "Pulsing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerMalfunctionComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(ConstructionSystem), typeof(WiresSystem) });
        SubscribeLocalEvent<PowerMalfunctionComponent, PowerMalfunctionRepairDoAfterEvent>(OnRepairFinished);
        SubscribeLocalEvent<PowerMalfunctionComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MalfAiHackedApcComponent, ComponentStartup>(OnHackStartup);
        SubscribeLocalEvent<MalfAiHackedApcComponent, ComponentShutdown>(OnHackShutdown);
    }

    /// <summary>
    /// Applies a persistent controller fault, optionally disconnecting the device from its power networks.
    /// </summary>
    public bool TrySetFault(Entity<PowerMalfunctionComponent> ent, bool disablePower)
    {
        if (TerminatingOrDeleted(ent) ||
            !TryComp<PowerNetworkBatteryComponent>(ent, out var battery) ||
            (disablePower && HasComp<HighPriorityAPCComponent>(ent)) ||
            (ent.Comp.Faulty && (!disablePower || ent.Comp.PowerDisabled)))
            return false;

        if (!ent.Comp.Faulty)
            ent.Comp.InsertedCables.Clear();

        ent.Comp.Faulty = true;
        ent.Comp.Revision++;
        if (disablePower && !ent.Comp.PowerDisabled)
        {
            ent.Comp.PreviouslyEnabled = battery.Enabled;
            ent.Comp.PowerDisabled = true;
            battery.Enabled = false;
        }

        DebugTools.Assert(!ent.Comp.PowerDisabled || !battery.Enabled);
        UpdateControllerIndicator(ent);
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(ent):target} suffered a power controller fault (power disabled: {disablePower}).");
        return true;
    }

    private void OnHackStartup(Entity<MalfAiHackedApcComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<PowerMalfunctionComponent>(ent, out var malfunction))
            malfunction.Revision++;

        _appearance.SetData(ent, ApcVisuals.Hacked, true);
    }

    private void OnHackShutdown(Entity<MalfAiHackedApcComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent))
            _appearance.SetData(ent, ApcVisuals.Hacked,
                TryComp<PowerMalfunctionComponent>(ent, out var malfunction) && malfunction.Faulty);
    }

    private void UpdateControllerIndicator(Entity<PowerMalfunctionComponent> ent)
    {
        _appearance.SetData(ent, ApcVisuals.Hacked, ent.Comp.Faulty || HasComp<MalfAiHackedApcComponent>(ent));
    }

    private bool NeedsRepair(Entity<PowerMalfunctionComponent> ent)
    {
        return ent.Comp.Faulty || HasComp<MalfAiHackedApcComponent>(ent);
    }

    private bool HasOpenPanel(EntityUid uid)
    {
        return TryComp<WiresPanelComponent>(uid, out var panel) && panel.Open;
    }

    private void OnInteractUsing(Entity<PowerMalfunctionComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !NeedsRepair(ent))
            return;

        var isCable = TryComp<StackComponent>(args.Used, out var cable) &&
            ent.Comp.RequiredCables.Contains(cable.StackTypeId);
        if (!isCable && !_tools.HasQuality(args.Used, RepairQuality))
            return;

        args.Handled = true;
        if (!HasOpenPanel(ent))
        {
            _popup.PopupEntity(Loc.GetString("power-malfunction-open-panel"), ent, args.User);
            return;
        }

        if (isCable)
        {
            TryInsertCable(ent, (args.Used, cable!), args.User);
            return;
        }

        if (!HasRequiredCables(ent))
        {
            _popup.PopupEntity(Loc.GetString("power-malfunction-missing-cables",
                ("cables", MissingCableNames(ent))), ent, args.User);
            return;
        }

        _tools.UseTool(args.Used, args.User, ent, (float) ent.Comp.RepairDelay.TotalSeconds,
            RepairQuality, new PowerMalfunctionRepairDoAfterEvent(ent.Comp.Revision));
    }

    private void TryInsertCable(Entity<PowerMalfunctionComponent> ent, Entity<StackComponent> cable, EntityUid user)
    {
        var type = cable.Comp.StackTypeId;
        if (ent.Comp.InsertedCables.Contains(type))
        {
            _popup.PopupEntity(Loc.GetString("power-malfunction-cable-already-inserted"), ent, user);
            return;
        }

        if (!_stack.TryUse((cable.Owner, cable.Comp), 1))
            return;

        ent.Comp.InsertedCables.Add(type);
        _popup.PopupEntity(Loc.GetString("power-malfunction-cable-inserted",
            ("cable", Loc.GetString(_prototypes.Index(type).Name))), ent, user);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} inserted one {type} into {ToPrettyString(ent):target} for repairs.");
    }

    private bool HasRequiredCables(Entity<PowerMalfunctionComponent> ent)
    {
        return ent.Comp.RequiredCables.IsSubsetOf(ent.Comp.InsertedCables);
    }

    private string MissingCableNames(Entity<PowerMalfunctionComponent> ent)
    {
        var names = new List<string>();
        foreach (var type in ent.Comp.RequiredCables)
        {
            if (!ent.Comp.InsertedCables.Contains(type))
                names.Add(Loc.GetString(_prototypes.Index(type).Name));
        }

        return string.Join(", ", names);
    }

    private void OnRepairFinished(Entity<PowerMalfunctionComponent> ent, ref PowerMalfunctionRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || TerminatingOrDeleted(ent) ||
            args.Revision != ent.Comp.Revision || !NeedsRepair(ent) || !HasOpenPanel(ent) ||
            !HasRequiredCables(ent) || args.Used is not { } tool || TerminatingOrDeleted(tool) ||
            !_tools.HasQuality(tool, RepairQuality) ||
            !TryComp<PowerNetworkBatteryComponent>(ent, out var battery))
            return;

        if (ent.Comp.PowerDisabled)
            battery.Enabled = ent.Comp.PreviouslyEnabled;

        ent.Comp.Faulty = false;
        ent.Comp.PowerDisabled = false;
        ent.Comp.InsertedCables.Clear();
        ent.Comp.Revision++;
        RemComp<MalfAiHackedApcComponent>(ent);
        UpdateControllerIndicator(ent);

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("power-malfunction-repaired"), ent, args.User);
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.User):user} repaired the power controller of {ToPrettyString(ent):target}.");
    }

    private void OnExamined(Entity<PowerMalfunctionComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !NeedsRepair(ent))
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.PowerDisabled
            ? "power-malfunction-examine-disabled"
            : "power-malfunction-examine-fault"));
        args.PushMarkup(Loc.GetString("power-malfunction-examine-repair"));
        if (HasOpenPanel(ent) && !HasRequiredCables(ent))
            args.PushText(Loc.GetString("power-malfunction-missing-cables", ("cables", MissingCableNames(ent))));
    }
}
