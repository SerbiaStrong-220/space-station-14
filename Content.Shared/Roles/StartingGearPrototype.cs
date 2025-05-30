using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Roles;

[Prototype]
public sealed partial class StartingGearPrototype : IPrototype, IInheritingPrototype, IEquipmentLoadout
{
    /// <inheritdoc/>
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<StartingGearPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [AbstractDataField]
    public bool Abstract { get; private set; }

    // SS220 fix inheritance begin
    /// <inheritdoc />
    //[DataField]
    //[AlwaysPushInheritance]
    //public Dictionary<string, EntProtoId> Equipment { get; set; } = new();

    /// <inheritdoc />
    [DataField]
    [AlwaysPushInheritance]
    public Dictionary<string, EntProtoId> Equipment
    {
        get
        {
            if (Parents != null)
            {
                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                Dictionary<string, EntProtoId> newDict = new();

                // Get values from parent
                // Since parents are the same prototype this action is essentially recursive.
                foreach (var parent in Parents)
                {
                    var parentProto = prototypeManager.Index<StartingGearPrototype>(parent);
                    foreach (var (pKey, pValue) in parentProto.Equipment)
                    {
                        newDict[pKey] = pValue;
                    }
                }

                // Add value from this prototype
                foreach (var (key, value) in _equipment)
                {
                    newDict[key] = value;
                }

                return newDict;
            }
            else
                return _equipment;
        }
        set => _equipment = value;
    }

    private Dictionary<string, EntProtoId> _equipment = new();
    // SS220 fix inheritance end

    /// <inheritdoc />
    [DataField]
    [AlwaysPushInheritance]
    public List<EntProtoId> Inhand { get; set; } = new();

    /// <inheritdoc />
    [DataField]
    [AlwaysPushInheritance]
    public Dictionary<string, List<EntProtoId>> Storage { get; set; } = new();
}

/// <summary>
/// Specifies the starting entity prototypes and where to equip them for the specified class.
/// </summary>
public interface IEquipmentLoadout
{
    /// <summary>
    /// The slot and entity prototype ID of the equipment that is to be spawned and equipped onto the entity.
    /// </summary>
    public Dictionary<string, EntProtoId> Equipment { get; set; }

    /// <summary>
    /// The inhand items that are equipped when this starting gear is equipped onto an entity.
    /// </summary>
    public List<EntProtoId> Inhand { get; set; }

    /// <summary>
    /// Inserts entities into the specified slot's storage (if it does have storage).
    /// </summary>
    public Dictionary<string, List<EntProtoId>> Storage { get; set; }

    /// <summary>
    /// Gets the entity prototype ID of a slot in this starting gear.
    /// </summary>
    public string GetGear(string slot)
    {
        return Equipment.TryGetValue(slot, out var equipment) ? equipment : string.Empty;
    }
}
