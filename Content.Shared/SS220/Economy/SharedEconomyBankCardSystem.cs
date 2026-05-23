// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Verbs;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Economy;

public abstract class SharedEconomyBankCardSystem : EntitySystem
{
    public static readonly SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(new("/Textures/SS220/Interface/VerbIcons/brain.png"));
    public const int PinCodeLength = 4;

    public override void Initialize()
    {
        SubscribeLocalEvent<EconomyBankCardComponent, GetVerbsEvent<Verb>>(OnVerb);
    }

    public void OnVerb(Entity<EconomyBankCardComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!TryComp<EconomySalaryReceiverComponent>(args.User, out var economySalaryReceiverComponent))
            return;

        var user = args.User;
        var verb = new Verb
        {
            Text = Loc.GetString("economy-ponder-for-data-verb-text"),
            Act = () =>
            {
                PonderForData((user, economySalaryReceiverComponent));
            },
            Icon = VerbIcon,
        };

        args.Verbs.Add(verb);
    }

    public abstract void PonderForData(Entity<EconomySalaryReceiverComponent> user);

}

[Serializable, NetSerializable]
public sealed class BankAccount(int accountId = default, int accountPin = default, int balance = default)
{
    public readonly int AccountId = accountId;

    public readonly int AccountPin = accountPin;

    public int Balance = balance;

    public string AccountOwnerName = string.Empty;
}
