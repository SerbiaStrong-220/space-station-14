using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Content.Server.SS220.Surgery.Components
{
    [RegisterComponent]
    public sealed partial class SurgicalInstrumentComponent : Component
    {
        /// <summary>
        /// Surgical instruments are used in surgical operations (including ghetto surgery)
        /// It is assumed that the tool has only one specialization, 
        /// but if we are going to create tools like "Incision Management System", then we will be able to assign several specializations at once
        /// </summary>

        [ViewVariables(VVAccess.ReadOnly)]
        public EntityUid? Target { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public SurgicalInstrumentMode Mode = SurgicalInstrumentMode.SELECTOR;

        /// <summary>
        /// Scalpels, knifes and etc
        /// </summary>
        [DataField("incision")]
        public bool Incision { get; set; }

        /// <summary>
        /// Saws, amputation knifes and etc
        /// </summary>
        [DataField("amputation")]
        public bool Amputation { get; set; }

        /// <summary>
        /// Tweezers...
        /// </summary>

        [DataField("extraction")]
        public bool Exctraction { get; set; }

        /// <summary>
        /// Hemostats
        /// </summary>

        [DataField("clamp")]
        public bool Clamp { get; set; }

        /// <summary>
        /// Retractors
        /// </summary>

        [DataField("retractor")]
        public bool Retractor { get; set; }

        /// <summary>
        ///  ????
        /// </summary>

        [DataField("drill")]
        public bool Drill { get; set; }

        /// <summary>
        /// Including chainsaw (lol)
        /// </summary>

        [DataField("saw")]
        public bool Saw { get; set; }

        /// <summary>
        /// Cauter, sigar
        /// </summary>

        [DataField("cauter")]
        public bool Cauter { get; set; }

        /// <summary>
        /// Read as "Used in surgical clearing"
        /// </summary>

        [DataField("debridement")]
        public bool Debridement { get; set; }



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
