using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.SS220.ArmorBlock;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArmorBlockComponent : Component
{
    /// <summary>
    /// The entity this armor protects
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Owner = null;
    /// <summary>
    /// The damage tresholds(a.k.a. resists)
    /// </summary>
    [DataField("tresholddict"), AutoNetworkedField]
    public Dictionary<string, FixedPoint2> TresholdDict = new Dictionary<string, FixedPoint2>();

    /// <summary>
    /// A list of armor damage tresholds(a.k.a. resist of the armor itself)
    /// </summary>
    [DataField("durabilitytresholddict"), AutoNetworkedField]
    public Dictionary<string, FixedPoint2> DurabilityTresholdDict = new Dictionary<string, FixedPoint2>();

    /// <summary>
    /// Specifies how much damage of a specified type whould be done on conversion
    /// </summary>
    //[DataField("transformmodifierdict"), AutoNetworkedField]
    //public Dictionary<string, FixedPoint2> TransformModifierDict = new Dictionary<string, FixedPoint2>();

    /// <summary>
    /// Specifies what types of damage should be converted to others
    /// </summary>
    [DataField("conversiondict"), AutoNetworkedField]
    public Dictionary<string, string> TransformSpecifierDict = new Dictionary<string, string>();
}
