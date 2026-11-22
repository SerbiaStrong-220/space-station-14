// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Ghost;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Ghost;

public sealed partial class GhostHudSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostHudSettingsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GhostHudSettingsComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<GhostHudSettingsComponent, GhostHudToggledMessage>(OnHudToggled);
    }

    private void OnMapInit(Entity<GhostHudSettingsComponent> ent, ref MapInitEvent args)
    {
        foreach (var setting in ent.Comp.Huds)
        {
            ApplyHud(ent.Owner, setting);
        }
    }

    private void OnUiOpened(Entity<GhostHudSettingsComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(GhostHudUiKey.Key))
            return;

        UpdateUi(ent);
    }

    private void OnHudToggled(Entity<GhostHudSettingsComponent> ent, ref GhostHudToggledMessage args)
    {
        if (args.Actor != ent.Owner)
            return;

        foreach (var setting in ent.Comp.Huds)
        {
            if (setting.Id != args.Hud)
                continue;

            if (setting.Enabled == args.Enabled)
                return;

            setting.Enabled = args.Enabled;
            ApplyHud(ent.Owner, setting);
            UpdateUi(ent);
            return;
        }
    }

    private void ApplyHud(EntityUid uid, GhostHudSetting setting)
    {
        var prototype = _prototype.Index(setting.Id);
        if (!setting.Enabled)
        {
            EntityManager.RemoveComponents(uid, prototype.Components);
            return;
        }

        EntityManager.AddComponents(uid, prototype.Components);
    }

    private void UpdateUi(Entity<GhostHudSettingsComponent> ent)
    {
        var huds = new List<GhostHudUiEntry>();
        foreach (var setting in ent.Comp.Huds)
        {
            var prototype = _prototype.Index(setting.Id);
            huds.Add(new GhostHudUiEntry(setting.Id, prototype.Name, setting.Enabled));
        }

        _ui.SetUiState(ent.Owner, GhostHudUiKey.Key, new GhostHudBoundUserInterfaceState(huds));
    }
}
