// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.SS220.Virology.Behaviors;
public sealed partial class VirusGunLockSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusGunLockComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    private void OnShotAttempted(Entity<VirusGunLockComponent> ent, ref ShotAttemptedEvent args)
    {
        _popup.PopupClient(Loc.GetString("pathology-strongman-no-guns"), ent, ent);
        args.Cancel();
    }
}
