using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.SS220.Vape;

namespace Content.Server.SS220.Vape;

public sealed class VapeSystem : SharedVapeSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VapeComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<VapeComponent, ClothingGotEquippedEvent>(OnEquipped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VapeComponent>();

        while (query.MoveNext(out var vape, out var comp))
        {
            if (comp.User == null)
                continue;

            if (comp.AtomizerEntity == null || comp.CartridgeEntity == null)
                continue;

            if (!comp.Puffing)
                continue;

            if (!Solution.TryGetRefillableSolution(comp.AtomizerEntity.Value, out _, out var sol))
                continue;

            if (!Solution.TryGetSolution(comp.User.Value, BloodstreamComponent.DefaultChemicalsSolutionName, out var userSol))
                continue;

            if (!TryComp<VapePartComponent>(comp.CartridgeEntity, out var cartPart))
                continue;

            if (cartPart.PartType is not CartridgePartData cartridge)
                continue;

            var inhaleAmount = cartridge.ConsumptionRate * frameTime;

            if (sol.Volume < inhaleAmount)
                inhaleAmount = sol.Volume.Float();

            if (inhaleAmount <= 0f)
            {
                comp.Puffing = false;
                comp.SoundEntity = Audio.Stop(comp.SoundEntity);
                Popup.PopupEntity(Loc.GetString("vape-empty-solution"), comp.User.Value, comp.User.Value);
                continue;
            }

            if (Solution.TryTransferSolution(userSol.Value, sol, inhaleAmount))
            {
                comp.AccumulatedVapedVolume += inhaleAmount;
            }

            cartridge.CurrentDurability -= cartridge.DurabilityConsumption * frameTime;

            if (cartridge.CurrentDurability <= 0f)
            {
                comp.Puffing = false;
                Popup.PopupEntity(Loc.GetString("vape-cartridge-no-durability"), comp.User.Value, comp.User.Value);
                Audio.Stop(comp.SoundEntity);
                cartridge.CurrentDurability = 0f;

                Dirty(comp.CartridgeEntity.Value, cartPart);
                continue;
            }

            Dirty(comp.CartridgeEntity.Value, cartPart);

            if (comp is { IsEmagged: false, StartPuffingTime: not null } &&
                GameTiming.CurTime > comp.StartPuffingTime + comp.MaxPuffTime)
            {
                var newDamage = new DamageSpecifier();

                foreach (var damage in comp.Damage.DamageDict)
                {
                    newDamage.DamageDict.Add(damage.Key, damage.Value.Float() * frameTime);
                }

                Damage.TryChangeDamage(comp.User.Value, newDamage, true);
            }

            Dirty(vape, comp);
        }
    }

    private void UpdatePuffing(Entity<VapeComponent> ent, Solution vapeSol)
    {
        if (ent.Comp.User == null)
            return;

        var actualVolume = ent.Comp.AccumulatedVapedVolume;

        if (actualVolume <= 0f)
        {
            ent.Comp.Puffing = false;

            Dirty(ent);
            return;
        }

        var environment = _atmos.GetContainingMixture(ent.Comp.User.Value, true, true);
        if (environment == null)
        {
            ent.Comp.AccumulatedVapedVolume = 0f;
            return;
        }

        var mixture = new GasMixture(actualVolume) { Temperature = vapeSol.Temperature };
        var volume = actualVolume / ent.Comp.ScaledVaporVolume;
        var temperature = vapeSol.Temperature;
        var pa = environment.Pressure * 1000f; // converting from kPa to Pa
        var moles = pa * volume / (Atmospherics.R * temperature);

        if (moles >= ent.Comp.MaxVaporVolume)
            moles = ent.Comp.MaxVaporVolume;

        mixture.SetMoles(ent.Comp.GasType, moles);

        _atmos.Merge(environment, mixture);
        ent.Comp.AccumulatedVapedVolume = 0f;

        Dirty(ent);
    }

    private void OnEquipped(Entity<VapeComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (MobState.IsIncapacitated(args.Wearer))
            return;

        if (ent.Comp.AtomizerEntity == null || ent.Comp.CartridgeEntity == null)
            return;

        if (!Solution.TryGetRefillableSolution(ent.Comp.AtomizerEntity.Value, out _, out var sol) ||
            sol.Volume <= 0f)
        {
            return;
        }

        ent.Comp.SoundEntity = Audio.PlayPvs(ent.Comp.VapingSound, ent)?.Entity;
        ent.Comp.StartPuffingTime = GameTiming.CurTime;
        ent.Comp.AccumulatedVapedVolume = 0f;
        ent.Comp.User = args.Wearer;
        ent.Comp.Puffing = true;

        Dirty(ent);
    }

    private void OnUnequipped(Entity<VapeComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.SoundEntity = Audio.Stop(ent.Comp.SoundEntity);

        if (ent.Comp.StartPuffingTime == null || ent.Comp.User == null)
            return;

        if (ent.Comp.AtomizerEntity == null || ent.Comp.CartridgeEntity == null)
            return;

        if (!Solution.TryGetRefillableSolution(ent.Comp.AtomizerEntity.Value, out var solComp, out var sol))
        {
            Popup.PopupEntity(Loc.GetString("vape-no-solution"), args.Wearer, args.Wearer);
            return;
        }

        Solution.UpdateChemicals(solComp.Value);
        UpdatePuffing(ent, sol);

        var flavor = Flavor.GetLocalizedFlavorsMessage(args.Wearer, sol);
        Popup.PopupEntity(flavor, args.Wearer, args.Wearer);

        ent.Comp.StartPuffingTime = null;
        ent.Comp.Puffing = false;
        ent.Comp.User = null;

        Dirty(ent);
    }
}
