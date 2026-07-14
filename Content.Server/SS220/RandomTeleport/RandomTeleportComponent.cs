// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Server.SS220.RandomTeleport;

/// <summary>
/// Allows you to teleport to a random entity with a specific component
/// </summary>
[RegisterComponent]
public sealed partial class RandomTeleportComponent : Component, ISerializationHooks
{
    [DataField(required: true)]
    public string? TargetsComponent;

    [DataField("whitelist")]
    public EntityWhitelist? TeleportTargetWhitelist;

    void ISerializationHooks.AfterDeserialization()
    {
        if (string.IsNullOrEmpty(TargetsComponent))
            throw new NullReferenceException("TargetsComponent string cannot be null or empty!");

        var factory = IoCManager.Resolve<IComponentFactory>();
        if (!factory.TryGetRegistration(TargetsComponent, out _))
            throw new Exception("Component not found");
    }
}
