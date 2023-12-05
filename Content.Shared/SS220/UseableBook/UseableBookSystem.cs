// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Popups;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Network;

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
        SubscribeLocalEvent<UseableBookCanReadEvent>(CanReadBook);
    }
    public void CanReadBook(UseableBookCanReadEvent args)
    {
        if (args.Cancelled)
            return;
        args.Handled = true;
        var book = args.BookComp;
        if (book.CanUseOneTime && book.Used)
        {
            args.Reason = Loc.GetString("useable-book-used-onetime"); // данную книгу можно было изучить только один раз
            goto failed;
        }
        if (book.LeftUses > 0)
        {
            args.Can = true;
            return;
        }

        args.Reason = Loc.GetString("useable-book-used"); // потрачены все использования
        failed:
        args.Cancelled = true;
    }
    public bool CanUseBook(UseableBookComponent comp, EntityUid user, [NotNullWhen(false)] out string? reason)
    {
        var args = new UseableBookCanReadEvent(user, comp);
        RaiseLocalEvent(args);
        reason = args.Reason;

        return args.Can && args.Handled;
    }

    private void OnBookUse(EntityUid entity, UseableBookComponent comp, UseInHandEvent args)
    {
        if (CanUseBook(comp, args.User, out var reason))
        {
            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(comp.ReadTime), new UseableBookReadDoAfterEvent(),
            entity, target: entity)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
            };
            _doAfter.TryStartDoAfter(doAfterEventArgs);

            return;
        }
        if (_net.IsClient)
            _popupSystem.PopupEntity(reason, entity, type: PopupType.Medium);
    }

    private void OnDoAfter(EntityUid uid, UseableBookComponent comp, UseableBookReadDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        if (args.Target is not {} target)
            return;
        
        comp.Used = true;
        comp.LeftUses -= 1;

        foreach (var kvp in comp.ComponentsOnRead)
        {
            var copiedComp = (Component) _serialization.CreateCopy(kvp.Value.Component, notNullableOverride: true);
            copiedComp.Owner = args.User;
            _entManager.AddComponent(args.User, copiedComp, true);
        }

        Dirty(comp);
        RaiseLocalEvent(new UseableBookOnReadEvent(args.User, comp));
    }
}