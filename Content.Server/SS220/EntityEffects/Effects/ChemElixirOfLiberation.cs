// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Server.SS220.CultYogg.Rave;
using Content.Server.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Server.SS220.CultYogg;
using Content.Shared.Humanoid;

namespace Content.Server.SS220.EntityEffects.Effects
{
    /// <summary>
    /// Used when someone eats MiGoShroom
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ChemElixirOfLiberation : EntityEffect
    {

        public override void Effect(EntityEffectBaseArgs args)
        {
            var entityManager = args.EntityManager;

            if (entityManager.TryGetComponent<CultYoggComponent>(args.TargetEntity, out var comp))
            {
                entityManager.System<CultYoggSystem>().NullifyShroomEffect(args.TargetEntity, comp);
                return;
            }
        }
        //ToDo check the guidebook
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-ss220-free-from-burden", ("chance", Probability));
    }
}
