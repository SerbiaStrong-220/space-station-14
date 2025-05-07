using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Verbs;
using Content.Server.SS220.Forensics;

namespace Content.Server.Forensics
{
    /// <summary>
    /// Used to transfer fingerprints from entities to forensic pads.
    /// </summary>
    public sealed class ForensicPadSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly LabelSystem _label = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ForensicPadComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<ForensicPadComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ForensicPadComponent, ForensicPadDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<ForensicPadComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);//SS220 Micro_fibers
        }

        private void OnExamined(EntityUid uid, ForensicPadComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            if (!component.Used)
            {
                args.PushMarkup(Loc.GetString("forensic-pad-unused"));
                return;
            }

            args.PushMarkup(Loc.GetString("forensic-pad-sample", ("sample", component.Sample)));
        }

        private void OnAfterInteract(EntityUid uid, ForensicPadComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            if (HasComp<ForensicScannerComponent>(args.Target))
                return;

            args.Handled = true;

            if (component.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-pad-already-used"), args.Target.Value, args.User);
                return;
            }

            //SS200 MicroFibers start
			/*
            if (_inventory.TryGetSlotEntity(args.Target.Value, "gloves", out var gloves))
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-pad-gloves", ("target", Identity.Entity(args.Target.Value, EntityManager))), args.Target.Value, args.User);
                return;
            }

            if (TryComp<FingerprintComponent>(args.Target, out var fingerprint) && fingerprint.Fingerprint != null)
            {
                if (args.User != args.Target)
                {
                    _popupSystem.PopupEntity(Loc.GetString("forensic-pad-start-scan-user", ("target", Identity.Entity(args.Target.Value, EntityManager))), args.Target.Value, args.User);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-pad-start-scan-target", ("user", Identity.Entity(args.User, EntityManager))), args.Target.Value, args.Target.Value);
                }
                StartScan(uid, args.User, args.Target.Value, component, fingerprint.Fingerprint);
                return;
            }

            if (TryComp<FiberComponent>(args.Target, out var fiber))
                StartScan(uid, args.User, args.Target.Value, component, string.IsNullOrEmpty(fiber.FiberColor) ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial)) : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial)));
			*/
			if (!TryGetSample(args.Target.Value, args.User, out var sample) || sample is null)//SS220 Micro_fibers
                return;

            StartScan(uid, args.User, args.Target.Value, component, sample);//SS220 Micro_fibers
			//SS200 MicroFibers end
        }

        private void StartScan(EntityUid used, EntityUid user, EntityUid target, ForensicPadComponent pad, string sample)
        {
            var ev = new ForensicPadDoAfterEvent(sample);

            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, pad.ScanDelay, ev, used, target: target, used: used)
            {
                NeedHand = true,
                BreakOnMove = true,
            };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        }

        //SS220 Micro_fibers start
        private void OnUtilityVerb(Entity<ForensicPadComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (HasComp<ForensicScannerComponent>(args.Target))
                return;

            // These need to be set outside for the anonymous method!
            var user = args.User;
            var target = args.Target;

            var verb = new UtilityVerb()
            {
                Act = () => AfterVerbClick(ent, target, user),
                IconEntity = GetNetEntity(ent),
                Text = Loc.GetString("forensic-pad-verb-text"),
                Message = Loc.GetString("forensic-pad-verb-message")
            };

            args.Verbs.Add(verb);
        }

        //Added so that all popups are after pressing the verb
        private void AfterVerbClick(Entity<ForensicPadComponent> ent, EntityUid target, EntityUid user)
        {
            if (ent.Comp.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-pad-already-used"), target, user);
                return;
            }

            if (!TryGetSample(target, user, out var sample) || sample is null)
                return;

            StartScan(ent, user, target, ent.Comp, sample);
        }

        private bool TryGetSample(EntityUid ent, EntityUid user, out string? sample)
        {
            sample = null;

            if (_inventory.TryGetSlotEntity(ent, "gloves", out var gloves))
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-pad-gloves", ("target", Identity.Entity(ent, EntityManager))), ent, user);
                return false;
            }

            if (TryComp<FingerprintComponent>(ent, out var fingerprint) && fingerprint.Fingerprint != null)
            {
                if (user != ent)
                {
                    _popupSystem.PopupEntity(Loc.GetString("forensic-pad-start-scan-user", ("target", Identity.Entity(ent, EntityManager))), ent, user);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-pad-start-scan-target", ("user", Identity.Entity(user, EntityManager))), ent, ent);
                }
                sample = fingerprint.Fingerprint;

                return true;
            }

            if (TryComp<FiberComponent>(ent, out var fiber))
            {
                sample = string.IsNullOrEmpty(fiber.FiberColor) ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial)) : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial));
                return true;
            }

            if (TryComp<MicroFiberComponent>(ent, out var microFiber))
            {
                sample = string.IsNullOrEmpty(microFiber.MicroFiberColor) ? Loc.GetString("forensic-micro-fibers", ("material", microFiber.MicroFiberMaterial)) : Loc.GetString("forensic-micro-fibers-colored", ("color", microFiber.MicroFiberColor), ("material", microFiber.MicroFiberMaterial));
                return true;
            }

            _popupSystem.PopupEntity(Loc.GetString("forensic-pad-verb-no-sapmles"), ent, user);
            return false;
        }
        //SS220 Micro_fibers end

        private void OnDoAfter(EntityUid uid, ForensicPadComponent padComponent, ForensicPadDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
            {
                return;
            }

            if (args.Args.Target != null)
            {
                string label = Identity.Name(args.Args.Target.Value, EntityManager);
                _label.Label(uid, label);
            }

            padComponent.Sample = args.Sample;
            padComponent.Used = true;

            args.Handled = true;
        }
    }
}
