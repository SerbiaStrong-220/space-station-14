using Content.Server.Clothing.Systems;
using Content.Server.Implants;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.PDA;

namespace Content.Server.Access.Systems;

/// <inheritdoc />
public sealed partial class AgentIdCardSystem : SharedAgentIdCardSystem
{
    [Dependency] private SharedIdCardSystem _card = default!;
    [Dependency] private ChameleonClothingSystem _chameleon = default!;
    [Dependency] private ChameleonControllerSystem _chamController = default!;

    [SubscribeLocalEvent]
    private void OnChameleonControllerOutfitChangedItem(Entity<AgentIDCardComponent> ent, ref InventoryRelayedEvent<ChameleonControllerOutfitSelectedEvent> args)
    {
        if (!TryComp<IdCardComponent>(ent, out var idCardComp))
            return;

        ProtoMan.Resolve(args.Args.ChameleonOutfit.Job, out var jobProto);

        var jobIcon = args.Args.ChameleonOutfit.Icon ?? jobProto?.Icon;
        var jobName = args.Args.ChameleonOutfit.Name ?? jobProto?.Name ?? "";

        if (jobIcon != null)
            _card.TryChangeJobIcon(ent, ProtoMan.Index(jobIcon.Value), idCardComp);

        if (jobName != "")
            _card.TryChangeJobTitle(ent, Loc.GetString(jobName), idCardComp);

        // If you have forced departments use those over the jobs actual departments.
        if (args.Args.ChameleonOutfit.Departments?.Count > 0)
            _card.TryChangeJobDepartment(ent, args.Args.ChameleonOutfit.Departments, idCardComp);
        else if (jobProto != null)
            _card.TryChangeJobDepartment(ent, jobProto, idCardComp);

        // Ensure that you chameleon IDs in PDAs correctly. Yes this is sus...

        // There is one weird interaction: If the job / icon don't match the PDAs job the chameleon will be updated
        // to the PDAs IDs sprite but the icon and job title will not match. There isn't a way to get around this
        // really as there is no tie between job -> pda or pda -> job.

        var idSlotGear = _chamController.GetGearForSlot(args, "id");
        if (idSlotGear == null)
            return;

        var proto = ProtoMan.Index(idSlotGear);
        if (!proto.TryComp<PdaComponent>(out var comp, EntityManager.ComponentFactory))
            return;

            var idSlotGear = _chamController.GetGearForSlot(args, "id");
            if (idSlotGear == null)
                return;

            var proto = _prototypeManager.Index(idSlotGear);
            if (!proto.TryGetComponent<PdaComponent>(out var comp, EntityManager.ComponentFactory))
                return;

            if (TryComp<ChameleonClothingComponent>(ent, out var chameleonComp) && chameleonComp.CanBeSetByController)
                _chameleon.SetSelectedPrototype(ent, comp.IdCard, component: chameleonComp);
        }

        private void OnVoiceMaskNameChanged(Entity<AgentIDCardComponent> ent, ref InventoryRelayedEvent<VoiceMaskNameUpdatedEvent> args)
        {
            if (!TryComp<IdCardComponent>(ent, out var idCard))
                return;

            if (!args.Args.VoiceMask.Comp.ChangeIDName)
                return;

            _cardSystem.TryChangeFullName(ent, args.Args.NewName, idCard);
        }

        private void OnAfterInteract(EntityUid uid, AgentIDCardComponent component, AfterInteractEvent args)
        {
            if (args.Target == null || !args.CanReach || _lock.IsLocked(uid) ||
                !TryComp<AccessComponent>(args.Target, out var targetAccess) || !HasComp<IdCardComponent>(args.Target))
                return;

            if (!TryComp<AccessComponent>(uid, out var access) || !HasComp<IdCardComponent>(uid))
                return;

            var beforeLength = access.Tags.Count;
            access.Tags.UnionWith(targetAccess.Tags);
            var addedLength = access.Tags.Count - beforeLength;

            _popupSystem.PopupEntity(Loc.GetString("agent-id-new", ("number", addedLength), ("card", args.Target)), args.Target.Value, args.User);
            if (addedLength > 0)
                Dirty(uid, access);
        }

        private void AfterUIOpen(EntityUid uid, AgentIDCardComponent component, AfterActivatableUIOpenEvent args)
        {
            if (!_uiSystem.HasUi(uid, AgentIDCardUiKey.Key))
                return;

            if (!TryComp<IdCardComponent>(uid, out var idCard))
                return;

            var state = new AgentIDCardBoundUserInterfaceState(idCard.FullName ?? "", idCard.LocalizedJobTitle ?? "", idCard.JobIcon);
            _uiSystem.SetUiState(uid, AgentIDCardUiKey.Key, state);
        }

        private void OnJobChanged(EntityUid uid, AgentIDCardComponent comp, AgentIDCardJobChangedMessage args)
        {
            if (!TryComp<IdCardComponent>(uid, out var idCard))
                return;

            // SS220 Radio-Job-Color-start
            var jobTitle = args.Job?.Trim() ?? string.Empty;
            _cardSystem.TryChangeJobTitle(uid, jobTitle, idCard);

            if (TryFindJobProtoFromJobName(jobTitle.ToLowerInvariant(), out var job))
                _cardSystem.TryChangeJobColor(uid, PresetIdCardSystem.GetJobColor(_prototypeManager, job), job.RadioIsBold);
            // SS220 Radio-Job-Color-end
        }

        private void OnNameChanged(EntityUid uid, AgentIDCardComponent comp, AgentIDCardNameChangedMessage args)
        {
            if (!TryComp<IdCardComponent>(uid, out var idCard))
                return;

            _cardSystem.TryChangeFullName(uid, args.Name, idCard);
        }

        private void OnJobIconChanged(EntityUid uid, AgentIDCardComponent comp, AgentIDCardJobIconChangedMessage args)
        {
            if (!TryComp<IdCardComponent>(uid, out var idCard))
                return;

            if (!_prototypeManager.Resolve(args.JobIconId, out var jobIcon))
                return;

            _cardSystem.TryChangeJobIcon(uid, jobIcon, idCard);

            if (TryFindJobProtoFromIcon(jobIcon, out var job))
                _cardSystem.TryChangeJobDepartment(uid, job, idCard);

            _jobStatus.UpdateStatus(Transform(uid).ParentUid);
        }

        private bool TryFindJobProtoFromIcon(JobIconPrototype jobIcon, [NotNullWhen(true)] out JobPrototype? job)
        {
          foreach (var jobPrototype in _prototypeManager.EnumeratePrototypes<JobPrototype>())
          {
              if (jobPrototype.Icon == jobIcon.ID)
              {
                  job = jobPrototype;
                  return true;
              }
          }

          job = null;
          return false;
        }

        // SS220 Radio-Job-Color-start
        private bool TryFindJobProtoFromJobName(string jobName, [NotNullWhen(true)] out JobPrototype? job)
        {
            foreach (var jobPrototype in _prototypeManager.EnumeratePrototypes<JobPrototype>())
            {
                if (jobPrototype.LocalizedName?.Trim() == jobName)
                {
                    job = jobPrototype;
                    return true;
                }
            }

            foreach (var jobPrototypePassenger in _prototypeManager.EnumeratePrototypes<JobPrototype>())
            {
                if (jobPrototypePassenger.LocalizedName == "пассажир")
                {
                    job = jobPrototypePassenger;
                    return true;
                }
            }

            job = null;
            return false;
        }
      // SS220 Radio-Job-Color-end
      }
}
