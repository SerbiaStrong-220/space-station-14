// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;

namespace Content.Shared.SS220.CultYogg.Lamp;
public abstract class SharedCultYoggLampSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    public override void Initialize()
    {
        base.Initialize();
    }
}
