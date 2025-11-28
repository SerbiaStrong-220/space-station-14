// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Popups;
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
}

public sealed partial class WeaponRestrictionMartialArtEffect : BaseMartialArtEffect
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
