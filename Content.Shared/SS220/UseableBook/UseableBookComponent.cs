// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.UseableBook;

[RegisterComponent]
public sealed partial class UseableBookComponent : Component
{
    [DataField()]
    [ViewVariables(VVAccess.ReadWrite)]
    public int LeftUses { get; set; } = 0;
    [DataField()]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanUseOneTime = false;
    [DataField()]
    [ViewVariables(VVAccess.ReadWrite)]
    public int ReadTime { get; private set; } = 120;
    [DataField()]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Used { get; set; } = false;
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public ComponentRegistry ComponentsOnRead { get; private set; } = new();
}

public partial class UseableBookEventArgs : HandledEntityEventArgs 
{
    public EntityUid Interactor { get; }
    public UseableBookComponent BookComp { get; }
    public string? Reason;
    public bool Can = false;
    public bool Cancelled = false;
    public UseableBookEventArgs(EntityUid interactor, UseableBookComponent book)
    {
        Interactor = interactor;
        BookComp = book;
    }
};
public sealed partial class UseableBookCanReadEvent : UseableBookEventArgs
{
    public UseableBookCanReadEvent(EntityUid interactor, UseableBookComponent book) : base(interactor, book) { }
};
public sealed partial class UseableBookOnReadEvent : UseableBookEventArgs
{
    public UseableBookOnReadEvent(EntityUid interactor, UseableBookComponent book) : base(interactor, book) { }
};