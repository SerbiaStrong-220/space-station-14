using Content.Shared.DoAfter;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Shared.SS220.Kpb;

[Serializable, NetSerializable]
public enum KpbScreenUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum KpbScreenCategory : byte
{
    FacialHair
}

[Serializable, NetSerializable]
public sealed class KpbScreenSelectMessage : BoundUserInterfaceMessage
{
    public KpbScreenSelectMessage(KpbScreenCategory category, string marking, int slot)
    {
        Category = category;
        Marking = marking;
        Slot = slot;
    }

    public KpbScreenCategory Category { get; }
    public string Marking { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class KpbScreenChangeColorMessage : BoundUserInterfaceMessage
{
    public KpbScreenChangeColorMessage(KpbScreenCategory category, List<Color> colors, int slot)
    {
        Category = category;
        Colors = colors;
        Slot = slot;
    }

    public KpbScreenCategory Category { get; }
    public List<Color> Colors { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class KpbScreenRemoveSlotMessage : BoundUserInterfaceMessage
{
    public KpbScreenRemoveSlotMessage(KpbScreenCategory category, int slot)
    {
        Category = category;
        Slot = slot;
    }

    public KpbScreenCategory Category { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class KpbScreenSelectSlotMessage : BoundUserInterfaceMessage
{
    public KpbScreenSelectSlotMessage(KpbScreenCategory category, int slot)
    {
        Category = category;
        Slot = slot;
    }

    public KpbScreenCategory Category { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class KpbScreenAddSlotMessage : BoundUserInterfaceMessage
{
    public KpbScreenAddSlotMessage(KpbScreenCategory category)
    {
        Category = category;
    }

    public KpbScreenCategory Category { get; }
}

[Serializable, NetSerializable]
public sealed class KpbScreenUiState : BoundUserInterfaceState
{
    public KpbScreenUiState(string species, List<Marking> facialHair, int facialHairSlotTotal)
    {
        Species = species;
        FacialHair = facialHair;
        FacialHairSlotTotal = facialHairSlotTotal;
    }

    public NetEntity Target;

    public string Species;

    public List<Marking> FacialHair;
    public int FacialHairSlotTotal;
}

[Serializable, NetSerializable]
public sealed partial class KpbScreenRemoveSlotDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public KpbScreenCategory Category;
    public int Slot;
}

[Serializable, NetSerializable]
public sealed partial class KpbScreenAddSlotDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public KpbScreenCategory Category;
}

[Serializable, NetSerializable]
public sealed partial class KpbScreenSelectDoAfterEvent : DoAfterEvent
{
    public KpbScreenCategory Category;
    public int Slot;
    public string Marking = string.Empty;

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class KpbScreenChangeColorDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public KpbScreenCategory Category;
    public int Slot;
    public List<Color> Colors = new List<Color>();
}

public sealed partial class KpbScreenActionEvent : InstantActionEvent
{
}
