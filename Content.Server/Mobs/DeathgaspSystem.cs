using Content.Server.Chat.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;
using Content.Shared.Popups;

namespace Content.Server.Mobs;

/// <see cref="DeathgaspComponent"/>
public sealed class DeathgaspSystem: EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeathgaspComponent, MobStateChangedEvent>(OnMobStateChanged);
    }
    
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    
    private void OnMobStateChanged(EntityUid uid, DeathgaspComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            _popupSystem.PopupEntity(Loc.GetString("death-reminder"), uid, uid, PopupType.LargeCaution);
        
        // don't deathgasp if they arent going straight from crit to dead
        if (args.NewMobState != MobState.Dead || args.OldMobState != MobState.Critical)
            return;

        Deathgasp(uid, component);
    }

    /// <summary>
    ///     Causes an entity to perform their deathgasp emote, if they have one.
    /// </summary>
    public bool Deathgasp(EntityUid uid, DeathgaspComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        _chat.TryEmoteWithChat(uid, component.Prototype, ignoreActionBlocker: true);

        return true;
    }
}
