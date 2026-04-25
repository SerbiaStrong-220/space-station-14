// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Economy;

public sealed class EconomyDeCentralBankSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<EconomyDeCentralBankComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EconomyDeCentralBankComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentRemove(Entity<EconomyDeCentralBankComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.IsCentralNode)
        {
            var enumerator = EntityQueryEnumerator<EconomyDeCentralBankComponent>();
            while (enumerator.MoveNext(out var _, out var comp))
            {
                if (comp is null || comp.IsCentralNode)
                    continue;

                comp.IsCentralNode = true;
                ent.Comp.Accounts.CopyTo([.. comp.Accounts], 0);
                break;
            }
        }
    }

    private void OnMapInit(Entity<EconomyDeCentralBankComponent> ent, ref MapInitEvent args)
    {
        var enumerator = EntityQueryEnumerator<EconomyDeCentralBankComponent>();

        var isCentralNodePresent = false;

        while (enumerator.MoveNext(out var _, out var comp) || isCentralNodePresent)
        {
            isCentralNodePresent = comp is not null && comp.IsCentralNode;
        }

        ent.Comp.IsCentralNode = !isCentralNodePresent;
    }
}
