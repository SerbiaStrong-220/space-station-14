// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Appearance;

public sealed class ToggleAppearanceOnUseSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleAppearanceOnUseComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ToggleAppearanceOnUseComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ToggleAppearanceOnUseComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, ToggleAppearanceOnUseComponent toggleComponent, ActivateInWorldEvent args)
    {
        SetAppearanceState((uid, toggleComponent), !toggleComponent.IsEnabled);
    }

    private void SetAppearanceState(Entity<ToggleAppearanceOnUseComponent?, AppearanceComponent?> entity, bool isEnabled)
    {
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2) || entity.Comp1.IsEnabled == isEnabled)
            return;

        entity.Comp1.IsEnabled = isEnabled;
        Dirty(entity, entity.Comp1);

        _appearance.SetData(entity, GenericOnOffVisual.Visual, entity.Comp1.IsEnabled ? GenericOnOffVisual.On : GenericOnOffVisual.Off, entity.Comp2);
    }

    private void OnGetState(EntityUid uid, ToggleAppearanceOnUseComponent toggleComponent, ref ComponentGetState args)
    {
        args.State = new ToggleAppearanceOnUseState(toggleComponent.IsEnabled);
    }

    private void OnHandleState(EntityUid uid, ToggleAppearanceOnUseComponent toggleComponent, ref ComponentHandleState args)
    {
        if (args.Current is not ToggleAppearanceOnUseState state)
            return;

        SetAppearanceState((uid, toggleComponent), state.IsEnabled);
    }
}

[Serializable, NetSerializable]
public enum GenericOnOffVisual : sbyte
{
    Visual,
    On,
    Off
}
