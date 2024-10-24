// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Popups;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Network;
using Content.Shared.Communications;

namespace Content.Shared.SS220.UseableBook;

public sealed class UseableBookSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseableBookComponent, UseInHandEvent>(OnBookUse);
        SubscribeLocalEvent<UseableBookComponent, UseableBookReadDoAfterEvent>(OnDoAfter);
    }
    public bool CanUseBook(EntityUid entity, UseableBookComponent comp, EntityUid user, [NotNullWhen(false)] out string? reason)
    {
        reason = null;
        bool bCan = false;

        if (comp.CanUseOneTime && comp.Used)
        {
            reason = Loc.GetString("useable-book-used-onetime"); // данную книгу можно было изучить только один раз
            goto retn;
        }
        if (comp.CustomCanRead is not null)
        {
            var customCanRead = comp.CustomCanRead;
            customCanRead.Interactor = user;
            customCanRead.BookComp = comp;
            RaiseLocalEvent(entity, (object)customCanRead, broadcast:true);

            if (customCanRead.Handled)
            {
                reason = customCanRead.Reason;
                bCan = customCanRead.Can;

                if (!customCanRead.Can)
                    goto retn;
            }
        }
        if (comp.LeftUses > 0)
            bCan = true;
        else
            reason = Loc.GetString("useable-book-used"); // потрачены все использования

        retn:
        return bCan;
    }

    private void OnBookUse(EntityUid entity, UseableBookComponent comp, UseInHandEvent args)
    {
        if (CanUseBook(entity, comp, args.User, out var reason))
        {
            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(comp.ReadTime), new UseableBookReadDoAfterEvent(),
            entity, target: entity)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
            };
            _doAfter.TryStartDoAfter(doAfterEventArgs);

            return;
        }
        if (_net.IsServer)
            _popupSystem.PopupEntity(reason, entity, type: PopupType.Medium);
    }

    private void OnDoAfter(EntityUid uid, UseableBookComponent comp, UseableBookReadDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        if (args.Target is not { } target)
            return;

        comp.Used = true;
        comp.LeftUses -= 1;

        foreach (var kvp in comp.ComponentsOnRead)
        {
            var copiedComp = (Component) _serialization.CreateCopy(kvp.Value.Component, notNullableOverride: true);
            copiedComp.Owner = args.User;
            _entManager.AddComponent(args.User, copiedComp, true);
        }

        Dirty(uid, comp);
        var useableArgs = new UseableBookOnReadEvent();
        useableArgs.Interactor = args.User;
        useableArgs.BookComp = comp;
        RaiseLocalEvent(useableArgs);
    }
}
