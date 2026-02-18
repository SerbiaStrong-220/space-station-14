// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Item;
using Robust.Shared.Serialization;
using Content.Shared.FCB.AltBlocking;

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

    public void OnActiveBlock(EntityUid uid, ChangeAppearanceOnActiveBlockingComponent component, ActiveBlockingEvent args)
    {
        component.Toggled = args.Active;
        Dirty(uid, component);
        UpdateVisuals((uid, component));
    }
}

[Serializable, NetSerializable]
public enum ActiveBlockingVisuals : byte
{
    Enabled,
    Layer,
    Color,
}
