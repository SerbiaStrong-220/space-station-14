// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System;
using System.Linq;
using Content.Server.Storage.EntitySystems;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.CultYogg;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Actions;
using Content.Server.Polymorph.Systems;
using Content.Shared.Popups;
using Content.Server.SS220.GameTicking.Rules;

namespace Content.Server.SS220.CultYogg;

public sealed class MiGoSystem : SharedMiGoSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}
