// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.AddSleepAction;

[RegisterComponent]
public sealed partial class AddSleepActionComponent : Component
{
    [DataField("sleepAction")] public EntityUid? SleepAction;
}
