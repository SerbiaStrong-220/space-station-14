// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.ActionBlocker;
using Content.Shared.SS220.Headset;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.RadioUIVerb;

public sealed partial class RadioUIVerbSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private ActionBlockerSystem _blockerSystem = default!;

    private static readonly LocId BuiAltVerb = "ui-radio-open";
    private static readonly HeadsetKey UiKey = HeadsetKey.Key;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioUIVerbComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<RadioUIVerbComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => InteractUI(user, ent),
            Text = Loc.GetString(BuiAltVerb),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
        });
    }

    private void InteractUI(EntityUid user, EntityUid uiEntity) //Activteable ui is hardcoded to a single ui key unfortunately((((
    {
        if (!_uiSystem.HasUi(uiEntity, UiKey))
            return;

        if (_uiSystem.IsUiOpen(uiEntity, UiKey, user))
        {
            _uiSystem.CloseUi(uiEntity, UiKey, user);
            return;
        }

        if (!_blockerSystem.CanInteract(user, uiEntity))
            return;

        _uiSystem.OpenUi(uiEntity, UiKey, user);
    }
}
