// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MalfAI;

public sealed partial class MalfAiHackApcEvent : EntityTargetActionEvent;

public sealed partial class MalfAiOpenStoreEvent : InstantActionEvent;

public sealed partial class MalfAiDeployTurretEvent : WorldTargetActionEvent;

public sealed partial class MalfAiBlackoutEvent : EntityTargetActionEvent;

public sealed partial class MalfAiAirFloodEvent : InstantActionEvent;

public sealed partial class MalfAiMachineOverrideEvent : EntityTargetActionEvent;

public sealed partial class MalfAiMachineOverloadEvent : EntityTargetActionEvent;

public sealed partial class MalfAiRobotFactoryEvent : WorldTargetActionEvent;

public sealed partial class MalfAiPowerSiphonEvent : EntityTargetActionEvent;

public sealed partial class MalfAiEmagEvent : EntityTargetActionEvent;

public sealed partial class MalfAiChaosPulseEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class MalfAiFactoryFeedDoAfterEvent : SimpleDoAfterEvent;
