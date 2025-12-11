// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
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

        var changedSignature = signature.Data == null || !signature.Data.Equals(args.Data);

        if (changedSignature)
        {
            signature.Data = args.Data;
            Dirty(ent.Owner, signature);
        }

        AfterSubmitSignature((ent.Owner, ent.Comp, signature), ref args, changedSignature);
    }

    protected virtual void AfterSubmitSignature(Entity<PaperComponent, SignatureComponent> ent, ref SignatureSubmitMessage args, bool changedSignature) { }
}
