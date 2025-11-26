using Content.Shared.Paper;

namespace Content.Shared.SS220.Signature;

public abstract class SharedSignatureSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PaperComponent, SignatureSubmitMessage>(OnSubmitSignature);
    }

    private void OnSubmitSignature(Entity<PaperComponent> ent, ref SignatureSubmitMessage args)
    {
        var signature = EnsureComp<SignatureComponent>(ent);

        signature.Data = args.Data;
        Dirty(ent.Owner, signature);
    }
}
