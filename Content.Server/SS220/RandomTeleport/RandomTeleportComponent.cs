// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Server.SS220.RandomTeleport;

/// <summary>
///     Teleports the target to a random destination entity with a configured component.
/// </summary>
[RegisterComponent]
public sealed partial class RandomTeleportComponent : Component, ISerializationHooks
{
    [DataField(required: true)]
    public string? DestinationComponentName;

    [DataField]
    public EntityWhitelist? DestinationWhitelist;

    void ISerializationHooks.AfterDeserialization()
    {
        if (string.IsNullOrEmpty(DestinationComponentName))
            throw new NullReferenceException("DestinationComponentName cannot be null or empty!");

        var factory = IoCManager.Resolve<IComponentFactory>();
        if (!factory.TryGetRegistration(DestinationComponentName, out _))
            throw new Exception($"Destination component '{DestinationComponentName}' was not found.");
    }
}
