using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Preferences.Managers;

public sealed partial class ServerPreferencesManager : IServerPreferencesManager, IPostInjectInit
{
    private bool AddsSponsorLoadout(HumanoidCharacterProfile profile, HumanoidCharacterProfile? old)
    {
        foreach (var (role, roleLoadout) in profile.Loadouts)
        {
            RoleLoadout? oldRoleLoadout = null;
            old?.Loadouts.TryGetValue(role, out oldRoleLoadout);

            foreach (var (group, loadouts) in roleLoadout.SelectedLoadouts)
            {
                var oldLoadouts = oldRoleLoadout?.SelectedLoadouts.GetValueOrDefault(group);
                foreach (var loadout in loadouts)
                {
                    if (oldLoadouts?.Contains(loadout) != true && IsSponsorLoadout(loadout.Prototype))
                        return true;
                }
            }
        }

        return false;
    }

    private bool IsSponsorLoadout(ProtoId<LoadoutPrototype> id)
    {
        var proto = _prototypeManager.Index(id);
        return proto.SponsorTierLoadoutEffects != null
                || proto.Effects.OfType<GroupLoadoutEffect>()
                    .Any(e => _prototypeManager.Index(e.Proto).SponsorTierLoadoutEffects != null);
    }

    public void RevalidatePreferences(ICommonSession session)
    {
        if (!_cachedPlayerPrefs.TryGetValue(session.UserId, out var prefsData) || prefsData.Prefs is not { } prefs)
            return;

        var sanitized = SanitizePreferences(session, prefs, _dependencies);
        var changed = false;
        foreach (var (slot, profile) in sanitized.Characters)
        {
            if (profile.Equals(prefs.Characters[slot]))
                continue;

            changed = true;
            if (ShouldStorePrefs(session.Channel.AuthType))
                _ = _db.SaveCharacterSlotAsync(session.UserId, profile, slot);
        }

        if (!changed)
            return;

        prefsData.Prefs = sanitized;
        _netManager.ServerSendMessage(new MsgPreferencesAndSettings
        {
            Preferences = sanitized,
            Settings = new GameSettings { MaxCharacterSlots = GetMaxUserCharacterSlots(session.UserId) }
        }, session.Channel);
    }

    // Corvax-Sponsors-Start: Calculate total available users slots with sponsors
    private int GetMaxUserCharacterSlots(NetUserId userId)
    {
        var maxSlots = _cfg.GetCVar(CCVars.GameMaxCharacterSlots);
        var extraSlots = _sponsors.TryGetInfo(userId, out var sponsor) ? sponsor.ExtraSlots : 0;
        return maxSlots + extraSlots;
    }
    // Corvax-Sponsors-End
}

