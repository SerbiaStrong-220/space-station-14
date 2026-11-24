// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Events;
using Content.Shared.Electrocution;

namespace Content.Shared.SS220.Bioware;

public sealed partial class BiowareSystem
{
    private void InitializeNerves()
    {
        SubscribeLocalEvent<NerveSplicingComponent, BeforeStaminaDamageEvent>(OnNerveSplicingStamina);
        SubscribeLocalEvent<NerveGroundingComponent, ElectrocutionAttemptEvent>(OnNerveGroundingElectrocution);
    }

    private void OnNerveSplicingStamina(Entity<NerveSplicingComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Value *= ent.Comp.StaminaCoefficient;
    }

    private void OnNerveGroundingElectrocution(Entity<NerveGroundingComponent> ent, ref ElectrocutionAttemptEvent args)
    {
        if (args.IgnoreInsulation)
            return;

        args.SiemensCoefficient = 0f;
    }
}
