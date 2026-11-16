// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server.SS220.CultYogg.Pond;

[RegisterComponent, Access(typeof(CultPondSystem))]
public sealed partial class CultPondComponent : Component //ToDo_SS220 figure out: do we need this component and shouldn't it be somethink more widely used
{
    [ViewVariables]
    public bool IsFilled = false;

    [DataField("solutionName", required: true)]
    public string Solution;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 AmountToAdd = FixedPoint2.New(10);

    [DataField, AutoNetworkedField]
    public TimeSpan RefillCooldown = TimeSpan.FromSeconds(15);

    /// <summary>
    /// For the future
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? RechargeSound;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? NextCharge;

    [ViewVariables(VVAccess.ReadWrite)]
    public ReagentQuantity? Reagent;
}
