// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.FCB.AltBlocking;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.FCB.ChangeAppearanceOnActiveBlocking;

public sealed partial class SharedChangeAppearanceOnActiveBlockingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangeAppearanceOnActiveBlockingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ChangeAppearanceOnActiveBlockingComponent, ActiveBlockingEvent>(OnActiveBlock);
        SubscribeLocalEvent<ChangeAppearanceOnActiveBlockingComponent, GotUnequippedHandEvent>(OnUnequip);
    }

    private void OnInit(Entity<ChangeAppearanceOnActiveBlockingComponent> ent, ref ComponentInit args)
    {
        UpdateVisuals(ent);
        Dirty(ent, ent.Comp);
    }

    public void UpdateVisuals(Entity<ChangeAppearanceOnActiveBlockingComponent> ent)
    {
        _appearanceSystem.SetData(ent, ActiveBlockingVisuals.Enabled, ent.Comp.Toggled);
    }

    public void OnActiveBlock(Entity<ChangeAppearanceOnActiveBlockingComponent> ent, ref ActiveBlockingEvent args)
    {
        bool toggleCheck = true;

        if(TryComp<ItemToggleComponent>(ent.Owner, out var toggleComp) && !toggleComp.Activated && ent.Comp.RequiresToggle)
            toggleCheck = false;

        ent.Comp.Toggled = args.Active && toggleCheck;
        Dirty(ent);
        UpdateVisuals((ent.Owner, ent.Comp));
    }

    public void OnUnequip(Entity<ChangeAppearanceOnActiveBlockingComponent> ent, ref GotUnequippedHandEvent args)
    {
        ent.Comp.Toggled = false;
        Dirty(ent);
        UpdateVisuals((ent.Owner, ent.Comp));
    }
}

[Serializable, NetSerializable]
public enum ActiveBlockingVisuals : byte
{
    Enabled,
    Layer,
    Color,
}
