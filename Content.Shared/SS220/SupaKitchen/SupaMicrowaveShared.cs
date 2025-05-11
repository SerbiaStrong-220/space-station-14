using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SupaKitchen;

[Serializable, NetSerializable]
public enum SupaMicrowaveState
{
    Idle,
    UnPowered,
    Cooking,
    Broken
}

[Serializable, NetSerializable]
public enum SupaMicrowaveVisualState
{
    Idle,
    Cooking,
    Broken
}

[Serializable, NetSerializable]
public enum SupaMicrowaveUiKey
{
    Key
}
