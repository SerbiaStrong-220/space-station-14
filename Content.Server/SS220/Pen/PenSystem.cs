using Content.Shared.Paper;
using Content.Shared.SS220.Pen;

namespace Content.Server.SS220.Pen;

public sealed class PenSystem : SharedPenSystem
{
    protected override void PasteSignatureAct(Entity<PaperComponent> paper, Entity<PenComponent> pen, EntityUid user)
    {
        base.PasteSignatureAct(paper, pen, user);
        Audio.PlayPvs(paper.Comp.Sound, paper);
    }
}
