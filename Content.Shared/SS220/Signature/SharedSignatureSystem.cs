// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Paper;

namespace Content.Shared.SS220.Signature;

public abstract class SharedSignatureSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PaperComponent, SignatureSubmitMessage>(OnSubmitSignature);
    }

    private void OnSubmitSignature(Entity<PaperComponent> ent, ref SignatureSubmitMessage args)
    {
        var signature = EnsureComp<SignatureComponent>(ent);

        signature.Data = args.Data;
        Dirty(ent.Owner, signature);

        _adminLog.Add(LogType.Chat, LogImpact.Medium, $"[Signature] User {ToPrettyString(args.Actor)} write {new SignatureLogData(signature.Data)} on {ToPrettyString(ent)}");
    }
}
