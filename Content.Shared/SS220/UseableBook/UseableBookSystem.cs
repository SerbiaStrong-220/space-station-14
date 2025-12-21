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
    }

    public bool CanUseBook(EntityUid entity, UseableBookComponent comp, EntityUid user, [NotNullWhen(false)] out string? reason)
    {
        reason = null;
        var bCan = false;

        if (comp is { CanUseOneTime: true, Used: true })
        {
            reason = Loc.GetString("useable-book-used-onetime"); // данную книгу можно было изучить только один раз
            return false;
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
                return customCanRead.Can;
            }
        }

        if (comp.LeftUses > 0)
            return true;

        reason = Loc.GetString("useable-book-used");
        return false;
    }

    private void OnBookUse(Entity<UseableBookComponent> ent, ref UseInHandEvent args)
    {
        if (CanUseBook(ent, ent.Comp, args.User, out var reason))
        {
            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(ent.Comp.ReadTime), new UseableBookReadDoAfterEvent(),
            ent, target: ent)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
            };
            _doAfter.TryStartDoAfter(doAfterEventArgs);

            return;
        }
        if (_net.IsServer)
            _popupSystem.PopupEntity(reason, ent, type: PopupType.Medium);
    }

    private void OnDoAfter(Entity<UseableBookComponent> ent, ref UseableBookReadDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        ent.Comp.Used = true;
        ent.Comp.LeftUses -= 1;

        foreach (var kvp in ent.Comp.ComponentsOnRead)
        {
            var copiedComp = (Component) _serialization.CreateCopy(kvp.Value.Component, notNullableOverride: true);
            copiedComp.Owner = args.User;
            _entManager.AddComponent(args.User, copiedComp, true);
        }

        Dirty(ent);

        var useableArgs = new UseableBookOnReadEvent
        {
            Interactor = args.User,
            BookComp = ent.Comp,
        };

        RaiseLocalEvent(useableArgs);
    }
}
