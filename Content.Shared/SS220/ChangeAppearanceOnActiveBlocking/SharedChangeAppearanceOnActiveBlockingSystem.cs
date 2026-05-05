// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.SS220.AltBlocking;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ChangeAppearanceOnActiveBlocking;

public sealed partial class SharedChangeAppearanceOnActiveBlockingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

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

    public void OnActiveBlock(Entity<ChangeAppearanceOnActiveBlockingComponent> ent, ref ActiveBlockingEvent args)
    {
        ent.Comp.Toggled = args.Active;
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
