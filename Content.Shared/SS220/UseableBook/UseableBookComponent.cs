// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.UseableBook;

[RegisterComponent, NetworkedComponent]
public sealed partial class UseableBookComponent : Component
{
    [DataField]
    public int LeftUses { get; set; } = 5;

    [DataField]
    public bool CanUseOneTime = false;

    [DataField]
    public int ReadTime { get; private set; } = 120;

    [DataField]
    public bool Used { get; set; } = false;

    [DataField(required: true)]
    public ComponentRegistry ComponentsOnRead { get; private set; } = new();

    [DataField]
    [NonSerialized]
    public UseableBookCanReadEvent? CustomCanRead;
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class UseableBookEventArgs : HandledEntityEventArgs
{
    public EntityUid Interactor { get; set; }
    public UseableBookComponent BookComp { get; set; } = default!;
    public string? Reason;
    public bool Can = false;
    public bool Cancelled = false;
};

public abstract partial class UseableBookCanReadEvent : UseableBookEventArgs { };
public sealed partial class UseableBookOnReadEvent : UseableBookEventArgs { };
