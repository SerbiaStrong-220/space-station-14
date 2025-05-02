using System.Linq;
using Content.Server.Construction.Conditions;
using Content.Server.Popups;
using Content.Server.SS220.MindSlave;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.SS220.MindSlave;
using Content.Shared.Tag; // SS220-mindslave
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Implants;

public sealed partial class ImplanterSystem : SharedImplanterSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MindSlaveSystem _mindslave = default!;
    [Dependency] private readonly TagSystem _tag = default!; // SS220-mindslave

    //SS220-mindslave begin
    [ValidatePrototypeId<EntityPrototype>]
    private const string MindSlaveImplantProto = "MindSlaveImplant";
    private const float MindShieldRemoveTime = 40;
    //SS220-mindslave end
    // SS220-fakeMS fix begin
    [ValidatePrototypeId<EntityPrototype>]
    private const string FakeMindShieldImplant = "FakeMindShieldImplant";
    // SS220-fakeMS fix end

    public override void Initialize()
    {
        base.Initialize();
        InitializeImplanted();

        SubscribeLocalEvent<ImplanterComponent, AfterInteractEvent>(OnImplanterAfterInteract);

        SubscribeLocalEvent<ImplanterComponent, ImplantEvent>(OnImplant);
        SubscribeLocalEvent<ImplanterComponent, DrawEvent>(OnDraw);
    }

    private void OnImplanterAfterInteract(EntityUid uid, ImplanterComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || args.Handled)
            return;

        var target = args.Target.Value;
        if (!CheckTarget(target, component.Whitelist, component.Blacklist))
            return;

        //SS220-mindslave begin
        if (component.ImplanterSlot.ContainerSlot != null
            && component.ImplanterSlot.ContainerSlot.ContainedEntity != null
            && HasComp<MindShieldImplantComponent>(component.ImplanterSlot.ContainerSlot.ContainedEntity)
            && _mindslave.IsEnslaved(target))
        {
            _popup.PopupEntity(Loc.GetString("mindshield-target-mindslaved"), target, args.User);
            return;
        }

        if (component.Implant == MindSlaveImplantProto)
        {
            if (args.User == target)
            {
                _popup.PopupEntity(Loc.GetString("mindslave-enslaving-yourself-attempt"), target, args.User);
                return;
            }

            if (_mindslave.IsEnslaved(target))
            {
                _popup.PopupEntity(Loc.GetString("mindslave-target-already-enslaved"), target, args.User);
                return;
            }

            if (HasComp<MindShieldComponent>(target))
            {
                _popup.PopupEntity(Loc.GetString("mindslave-target-mindshielded"), target, args.User);
                return;
            }
        }
        //SS220-mindslave end

        // SS220-fakeMSfix begin
        if (component.Implant == FakeMindShieldImplant)
        {
            if (HasComp<MindShieldComponent>(target))
            {
                _popup.PopupEntity(Loc.GetString("mindslave-target-mindshielded"), args.User);
                return;
            }
            if (_mindslave.IsEnslaved(target))
            {
                _popup.PopupEntity(Loc.GetString("mindshield-target-mindslaved"), target, args.User);
                return;
            }
        }
        // SS220-fakeMSfix end
        //TODO: Rework when surgery is in for implant cases
        if (component.CurrentMode == ImplanterToggleMode.Draw && !component.ImplantOnly)
        {
            TryDraw(component, args.User, target, uid);
        }
        else
        {
            if (!CanImplant(args.User, target, uid, component, out var implant, out _))
            {
                // no popup if implant doesn't exist
                if (implant == null)
                    return;

                // show popup to the user saying implant failed
                var name = Identity.Name(target, EntityManager, args.User);
                var msg = Loc.GetString("implanter-component-implant-failed", ("implant", implant), ("target", name));
                _popup.PopupEntity(msg, target, args.User);
                // prevent further interaction since popup was shown
                args.Handled = true;
                return;
            }

            //Implant self instantly, otherwise try to inject the target.
            if (args.User == target)
                Implant(target, target, uid, component);
            else
                TryImplant(component, args.User, target, uid);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Attempt to implant someone else.
    /// </summary>
    /// <param name="component">Implanter component</param>
    /// <param name="user">The entity using the implanter</param>
    /// <param name="target">The entity being implanted</param>
    /// <param name="implanter">The implanter being used</param>
    public void TryImplant(ImplanterComponent component, EntityUid user, EntityUid target, EntityUid implanter)
    {
        var args = new DoAfterArgs(EntityManager, user, component.ImplantTime, new ImplantEvent(), implanter, target: target, used: implanter)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);

        var userName = Identity.Entity(user, EntityManager);
        _popup.PopupEntity(Loc.GetString("implanter-component-implanting-target", ("user", userName)), user, target, PopupType.LargeCaution);
    }

    /// <summary>
    /// Try to remove an implant and store it in an implanter
    /// </summary>
    /// <param name="component">Implanter component</param>
    /// <param name="user">The entity using the implanter</param>
    /// <param name="target">The entity getting their implant removed</param>
    /// <param name="implanter">The implanter being used</param>
    //TODO: Remove when surgery is in
    public void TryDraw(ImplanterComponent component, EntityUid user, EntityUid target, EntityUid implanter)
    {
        //SS220-Mindshield-remove-time begin
        var isMindShield = false;

        if (_container.TryGetContainer(target, ImplanterComponent.ImplantSlotId, out var implantContainer))
        {
            foreach (var implant in implantContainer.ContainedEntities)
            {
                if (HasComp<MindShieldImplantComponent>(implant) && _container.CanRemove(implant, implantContainer))
                {
                    isMindShield = true;
                    break;
                }
            }
        }
        var delay = isMindShield ? MindShieldRemoveTime : component.DrawTime;
        var popupPath = isMindShield ? "injector-component-drawing-mind-shield" : "injector-component-drawing-user";
        var args = new DoAfterArgs(EntityManager, user, delay, new DrawEvent(), implanter, target: target, used: implanter)
        // var args = new DoAfterArgs(EntityManager, user, component.DrawTime, new DrawEvent(), implanter, target: target, used: implanter)
        //SS220-Mindshield-remove-time end
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(args))
            // _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user); //SS220-Mindshield-remove-time
            _popup.PopupEntity(Loc.GetString(popupPath), target, user);


    }

    private void OnImplant(EntityUid uid, ImplanterComponent component, ImplantEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        Implant(args.User, args.Target.Value, args.Used.Value, component);

        args.Handled = true;
    }

    private void OnDraw(EntityUid uid, ImplanterComponent component, DrawEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used == null || args.Target == null)
            return;

        Draw(args.Used.Value, args.User, args.Target.Value, component);

        args.Handled = true;
    }
}
