// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;

namespace Content.Shared.SS220.Medical;

[RegisterComponent]
public sealed partial class BlueShieldMonitorComponent : Component
{
    /// <summary>
    ///     Job prototype IDs considered high clearance. Always shown on the Blue Shield monitor.
    /// </summary>
    /// <remarks>
    ///     Matched against two sources: the job role on the person's mind (server-assigned, unforgeable)
    ///     and the JobPrototype of their ID card, but only when the card is not an agent ID card.
    ///     Agent ID cards let their owner rewrite card job data at will, so their card data is never
    ///     trusted; disguised agents are detected via <see cref="HighClearanceIcons"/> instead.
    /// </remarks>
    [DataField]
    public HashSet<string> HighClearanceProtos = new()
    {
        "captain",
        "headofpersonnel",
        "chiefengineer",
        "chiefmedicalofficer",
        "headofsecurity",
        "quartermaster",
        "researchdirector",
        "blueshield",
        "nanotrasenrepresentative",
    };

    /// <summary>
    ///     Job icon prototype IDs for high clearance roles. Used to detect agents using chameleon to disguise as heads.
    /// </summary>
    [DataField]
    public HashSet<string> HighClearanceIcons = new()
    {
        "JobIconCaptain",
        "JobIconHeadOfPersonnel",
        "JobIconChiefMedicalOfficer",
        "JobIconHeadOfSecurity",
        "JobIconQuarterMaster",
        "JobIconResearchDirector",
        "JobIconBlueShield",
        "JobIconNanotrasen",
        "JobIconChiefEngineer",
    };

    /// <summary>
    ///     Components that mark a suit sensor as a subdermal tracking implant.
    ///     Only people which are currently implanted with such an implant can be listed as unidentified.
    /// </summary>
    [DataField]
    public EntityWhitelist TrackingImplantWhitelist = new()
    {
        Components = new[] { "SubdermalImplant" },
    };
}
