using System.Diagnostics.CodeAnalysis;
using Content.Server.GameTicking.Events;
using Content.Shared.UserInterface;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Players;
using Content.Shared.SS220.Discord;
using Content.Shared.SS220.Shlepovend;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Shlepovend;

public sealed class ShlepovendSystem : SharedShlepovendSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        // UI
        SubscribeLocalEvent<ShlepovendComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<ShlepovendComponent, ShlepovendPurchaseMsg>(OnPurchaseAttempt);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }

    public bool TryGetInitialTokenValue(ICommonSession player, [NotNullWhen(true)] out int? initialTokens)
    {
        initialTokens = null;

        var sponsorInfo = player.ContentData()?.SponsorInfo;
        if (sponsorInfo is null)
            return false;

        var highestAvailableTier = GetHighestTier(sponsorInfo.Tiers);
        if (highestAvailableTier == null)
            return false;

        initialTokens = highestAvailableTier.RoundstartTokens;
        return true;
    }

    public void SendTokenCount(ICommonSession player)
    {
        var contentPlayerData = player.ContentData();
        if (contentPlayerData == null)
            return;

        if (!contentPlayerData.ShlepovendTokens.HasValue)
        {
            if (TryGetInitialTokenValue(player, out var initialTokens))
                contentPlayerData.ShlepovendTokens = initialTokens;
            else
                contentPlayerData.ShlepovendTokens = 0;
        }

        RaiseNetworkEvent(new ShlepovendTokenAmountMsg() { Tokens = contentPlayerData.ShlepovendTokens.Value }, player);
    }

    // Reset token count on round restart
    public void OnRoundStart(RoundStartingEvent args)
    {
        foreach (var playerData in _player.GetAllPlayerData())
        {
            var contentData = playerData.ContentData();
            if (contentData is null)
                continue;

            contentData.ShlepovendTokens = null;
        }
    }

    public void OnToggleInterface(Entity<ShlepovendComponent> entity, ref AfterActivatableUIOpenEvent args)
    {
        if (TryComp<ActorComponent>(args.Actor, out var actor) && actor.PlayerSession is { } session)
            SendTokenCount(session);
    }

    public void OnPurchaseAttempt(Entity<ShlepovendComponent> entity, ref ShlepovendPurchaseMsg args)
    {
        // check that group exists
        if (!_prototype.TryIndex(args.GroupId, out var groupProto))
            return;

        // check that item in question is indeed in that group
        if (!groupProto.Rewards.TryGetValue(args.ItemId, out var price))
            return;

        // check if item prototype exists
        if (!_prototype.TryIndex(args.ItemId, out _))
            return;

        if (!TryComp<ActorComponent>(args.Actor, out var actorComp) || actorComp.PlayerSession is not { } session)
            return;

        // check if there is required information about the player
        if (session.ContentData() is not { } contentData)
            return;

        if (contentData.SponsorInfo is not { } sponsorInfo)
            return;

        // Check if player got discord role that is required by reward group
        var gotRequiredRole = false;
        foreach (var tier in sponsorInfo.Tiers)
        {
            if (groupProto.RequiredRole == null)
                break;

            gotRequiredRole = groupProto.IsExactRoleRequired ? tier == groupProto.RequiredRole :
                (int)tier >= (int)groupProto.RequiredRole;

            if (gotRequiredRole)
                break;
        }
        if (!gotRequiredRole)
            return;

        // Check if player got enough tokens left in this round
        if (!contentData.ShlepovendTokens.HasValue || contentData.ShlepovendTokens.Value < price)
            return;

        // Purchase finally happens
        var item = Spawn(args.ItemId, Transform(args.Actor).Coordinates);
        _hands.TryPickupAnyHand(args.Actor, item); //try put purchased item into hand

        contentData.ShlepovendTokens -= price;
        SendTokenCount(session);
        _audio.PlayPvs(entity.Comp.SoundVend, entity);
    }
}
