// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.MartialArts.Effects;

public sealed partial class WeaponRestrictionMartialArtEffectSystem : BaseMartialArtEffectSystem<WeaponRestrictionMartialArtEffect>
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MartialArtistComponent, AttemptMeleeUserEvent>(OnAttemptAttack);
        SubscribeLocalEvent<MartialArtistComponent, ThrowAttemptEvent>(OnThrowAttempt);
    }

    private void OnAttemptAttack(EntityUid uid, MartialArtistComponent artist, ref AttemptMeleeUserEvent ev)
    {
        if (ev.Cancelled)
            return;

        if (!TryEffect(uid, out var effect))
            return;

        if ((effect.Whitelist == null || _whitelist.IsWhitelistPass(effect.Whitelist, ev.Weapon)) && (effect.Blacklist == null || _whitelist.IsWhitelistFail(effect.Blacklist, ev.Weapon)))
            return;

        _popup.PopupClient(Loc.GetString("martial-art-effects-weapon-restriction-popup"), uid, uid);

        ev.Cancelled = true;
    }

    private void OnThrowAttempt(EntityUid uid, MartialArtistComponent artist, ref ThrowAttemptEvent ev)
    {
        if (ev.Cancelled)
            return;

        if (!TryEffect(uid, out var effect))
            return;

        if ((effect.Whitelist == null || _whitelist.IsWhitelistPass(effect.Whitelist, ev.ItemUid)) && (effect.Blacklist == null || _whitelist.IsWhitelistFail(effect.Blacklist, ev.ItemUid)))
            return;

        // the throw attempt being raised only on server, when it will be changed by officials (i'm sure about that) you probably should change method to PopupClient - Lokilife 05.12.2025
        _popup.PopupEntity(Loc.GetString("martial-art-effects-weapon-restriction-popup"), uid, uid);

        ev.Cancel();
    }
}

public sealed partial class WeaponRestrictionMartialArtEffect : MartialArtEffectBase<WeaponRestrictionMartialArtEffect>
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// When true the items that fails whitelist or blacklist cannot be thrown
    /// </summary>
    [DataField]
    public bool NoThrow = true;
}
