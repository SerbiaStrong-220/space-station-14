using Robust.Shared.Audio;
using Content.Shared.Whitelist;

namespace Content.Server.SS220.BeerUpdate.MaltGrinder;

[RegisterComponent]
public sealed partial class MaltGrinderComponent : Component
{
    [DataField]
    public int StorageMaxEntities = 6;

    [DataField]
    public TimeSpan WorkTime = TimeSpan.FromSeconds(3.5);

    [DataField]
    public SoundSpecifier ClickSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public SoundSpecifier WorkSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

    [DataField]
    public EntityWhitelist InputWhitelist = new();
}

[RegisterComponent]
public sealed partial class ActiveMaltGrinderComponent : Component
{
    [ViewVariables]
    public TimeSpan EndTime;
}
