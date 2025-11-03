using Content.Shared.Popups;
using Content.Shared.Animals.Components;
using Content.Shared.Body.Events;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Animals.Systems;

public sealed class MouthContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MouthContainerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MouthContainerComponent, BeingGibbedEvent>(OnEntityGibbedEvent);
        SubscribeLocalEvent<MouthContainerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<ItemComponent, GetVerbsEvent<Verb>>(OnGetVerbItem);

        base.Initialize();
    }

    private void OnStartup(EntityUid uid, MouthContainerComponent component, ComponentStartup args)
    {
        component.MouthSlot = _container.EnsureContainer<ContainerSlot>(uid, component.MouthSlotId);
    }

    private void OnGetVerb(Entity<MouthContainerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var toInsert = _hands.GetActiveItem(args.User);
        if (CanInsert(ent, toInsert, ent)) //&& toInsert != null && _whitelistSystem.IsWhitelistFail(ent.Comp.EquipmentWhitelist, toInsert.Value)
        {
            var v = new Verb
            {
                Priority = 1,
                Text = "Поместить в рот",
                Disabled = false,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () =>
                {
                    TryInsert(ent, toInsert, ent);
                },
            };
            args.Verbs.Add(v);
        }
        else
        {
            if (ent.Comp.MouthSlot.ContainedEntity != null)
            {
                var v = new Verb
                {
                    Priority = 1,
                    Text = "Вынуть изо рта",
                    Disabled = false,
                    Impact = LogImpact.Medium,
                    DoContactInteraction = true,
                    Act = () =>
                    {
                        {
                            TryEject(ent, ent);
                        }
                    },
                };
                args.Verbs.Add(v);
            }
        }
    }

    private void OnGetVerbItem(Entity<ItemComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var subject = args.User;
        if (!HasComp<MouthContainerComponent>(subject))
            return;
        var mouthComp = Comp<MouthContainerComponent>(subject);
        var toInsert = ent.Owner;

        if (IsEmpty(mouthComp, subject) && subject != toInsert) // && _whitelistSystem.IsWhitelistFail(mouthComp.EquipmentWhitelist, toInsert)
        {
            var v = new Verb
            {
                Priority = 1,
                Text = "Поместить в рот",
                Disabled = false,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () =>
                {
                    TryInsert(subject, toInsert, mouthComp);
                },
            };
            args.Verbs.Add(v);
        }
    }

    private void OnEntityGibbedEvent(Entity<MouthContainerComponent> ent, ref BeingGibbedEvent args)
    {
        TryEject(ent, ent);
    }
    public bool TryInsert(EntityUid uid, EntityUid? toInsert, MouthContainerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (toInsert == null || component.MouthSlot.ContainedEntity == toInsert)
            return false;
        if (!CanInsert(uid, toInsert.Value, component))
            return false;
        if (_whitelistSystem.IsWhitelistFail(component.EquipmentWhitelist, toInsert.Value))
            return false;

        _container.Insert(toInsert.Value, component.MouthSlot);
        UpdateAppearance(uid, component);
        _popup.PopupPredicted("Поместил", uid, uid);
        return true;
    }
    public bool TryEject(EntityUid uid, MouthContainerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.MouthSlot.ContainedEntity == null)
            return false;

        var toremove = component.MouthSlot.ContainedEntity.Value;

        _container.RemoveEntity(uid, toremove);
        UpdateAppearance(uid, component);
        _popup.PopupPredicted("Вынул", uid, uid);
        return true;
    }

    private void UpdateAppearance(EntityUid uid, MouthContainerComponent component)
    {
        Appearance.SetData(uid, MouthContainerVisuals.Stored, component.MouthSlot.ContainedEntity != null);
    }


    private bool CanInsert(EntityUid uid, EntityUid? toInsert, MouthContainerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (toInsert == null)
            return false;

        return IsEmpty(component, uid);
    }

    private bool IsEmpty(MouthContainerComponent component, EntityUid uid)
    {
        return component.MouthSlot.ContainedEntity == null;
    }
}
