using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Server.SS220.SpiderQueen.Systems.SpiderQueenInterfaceSystem;

[Serializable, NetSerializable]
public enum SpiderQueenSpawnKey
{
    Key,
}

public sealed partial class SpiderOpenSpawnMenuAction : InstantActionEvent { }
