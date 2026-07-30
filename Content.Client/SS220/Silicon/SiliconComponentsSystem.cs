// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SS220.SiliconComponents;
using Content.Shared.Wires;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Client.SS220.SiliconComponents;

public sealed partial class SiliconComponentsSystem : SharedSiliconComponentsSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void UpdateUI(Entity<SiliconComponentsComponent?> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, SiliconUiKey.Key, out var bui))
            bui.Update();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }
}
