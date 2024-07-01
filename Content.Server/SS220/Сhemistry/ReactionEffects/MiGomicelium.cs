// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Server.Medical;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.CultYogg;
using Content.Shared.Humanoid;

namespace Content.Server.SS220.Chemistry.ReactionEffects
{
    /// <summary>
    /// Used when someone eats MiGoShroom
    /// </summary>
    [UsedImplicitly]
    public sealed partial class MiGomiceliumEffect : ReagentEffect //stub as vomit, will change when figure out
    {

        [Dependency] private readonly IEntityManager _entityManager = default!;

        /// How many units of thirst to add each time we vomit
        [DataField]
        public float ThirstAmount = -8f;
        /// How many units of hunger to add each time we vomit
        [DataField]
        public float HungerAmount = -8f;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-chem-vomit", ("chance", Probability));
        public override void Effect(ReagentEffectArgs args)
        {
            //strange errors.
            /*
            if (!_entityManager.TryGetComponent<CultYoggComponent>(args.SolutionEntity, out var comp))
            {
                //will do it later
                //var ev = new CultYoggShroomEatenEvent();
                //_entityManager.RaiseLocalEvent(args.SolutionEntity, ref ev);
                return;
            }
            */
            //idk why it isn't working
            /*
            if (!HasComp<HumanoidAppearanceComponent>(args.SolutionEntity))
            {
                //create system ti corrupt an animal
                //AnimalCorruption((EntityUid) args.Target);//beast must be transformed
                return;
            }
            */

            if (args.Scale != 1f)
                return;

            var vomitSys = args.EntityManager.EntitySysManager.GetEntitySystem<VomitSystem>();

            vomitSys.Vomit(args.SolutionEntity, ThirstAmount, HungerAmount);
        }
    }
}
