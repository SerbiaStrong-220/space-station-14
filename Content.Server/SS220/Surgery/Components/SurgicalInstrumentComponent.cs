using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Content.Server.SS220.Surgery.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Content.Shared.SS220.Surgery.Prototypes;

namespace Content.Server.SS220.Surgery.Components
{
    /// <summary>
    /// Surgical instruments are used in surgical operations (including ghetto surgery)
    /// It is assumed that the tool has only one specialization, 
    /// but if we are going to create tools like "Incision Management System", then we will be able to assign several specializations at once
    /// </summary>
    [RegisterComponent]
    public sealed partial class SurgicalInstrumentComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public EntityUid? Target { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public SurgicalInstrumentMode Mode = SurgicalInstrumentMode.SELECTOR;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("specialization", customTypeSerializer: typeof(PrototypeIdListSerializer<SurgicalInstrumentSpecializationTypePrototype>))]
        public List<string> Specialization { get; set; }

        [DataField("succesfullStepChance")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SuccesfullStepChance = 1.0f;

        [DataField("usageTime")] // For DoAfter
        [ViewVariables(VVAccess.ReadWrite)]
        public float UsageTime = 1.5f;
    }

    public enum SurgicalInstrumentMode
    {
        OPERATION,
        SELECTOR
    }

    public enum SurgicalInstrumentsSpecializations
    {
        incision,
        amputation,
        cauter,
        clamp,
        retractor
    }
}
