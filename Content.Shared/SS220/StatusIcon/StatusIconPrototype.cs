using Content.Shared.Stealth.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Content.Shared.StatusIcon;

namespace Content.Shared.SS220.StatusIcon;


/// <summary>
/// StatusIcons for the med HUD
/// </summary>
[Prototype]
public sealed partial class LimitationReviveIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<LimitationReviveIconPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}
