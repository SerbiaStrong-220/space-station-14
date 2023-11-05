using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Content.Server.SS220.Surgery.Components
{
    [RegisterComponent]
    public sealed partial class SurgicalInstrumentComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public EntityUid? Target { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public SurgicalInstrumentMode Mode = SurgicalInstrumentMode.SELECTOR;

        /// <summary>
        /// Tool qualification
        /// </summary>

        [DataField("incision")] // scalpels, knifes etc
        public bool Incision { get; set; }

        [DataField("amputation")] // saws, amputation knife
        public bool Amputation { get; set; }

        [DataField("extraction")] // tweezers etc
        public bool Exctraction { get; set; }

        [DataField("clamp")] // hemostat etc
        public bool Clamp { get; set; }

        [DataField("retractor")] // retractor etc
        public bool Retractor { get; set; }

        [DataField("drill")] // drill
        public bool Drill { get; set; }

        [DataField("saw")] // saw, chainsaw (lol) etc
        public bool Saw { get; set; }

        [DataField("cauter")] // caouter, sigar etc
        public bool Cauter { get; set; }

        [DataField("debridement")] // Read as "Used in surgical clearing"
        public bool Debridement { get; set; }

        /// <summary>
        /// Properties
        /// </summary>

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
}
