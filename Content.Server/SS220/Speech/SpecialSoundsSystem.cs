// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Speech;
using Content.Shared.Verbs;
using Content.Server.Popups;
using Content.Shared.Inventory;

namespace Content.Server.SS220.Speech;

public sealed class SpecialSoundsSystem : EntitySystem
{

    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpecialSoundsComponent, GetVerbsEvent<Verb>>(OnVerb);
    }

    private void OnVerb(EntityUid uid, SpecialSoundsComponent component, GetVerbsEvent<Verb> args)
    {
        // standard interaction checks
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!_inventorySystem.TryGetContainingSlot(uid, out _))
            return;

        args.Verbs.UnionWith(new[]
        {
            CreateVerb(uid, component, args.User, SpecialSoundMode.SpecialSoundOff),
            CreateVerb(uid, component, args.User, SpecialSoundMode.SpecialSoundOn),
        });
    }

    private Verb CreateVerb(EntityUid uid, SpecialSoundsComponent component, EntityUid userUid, SpecialSoundMode mode)
    {
        return new Verb()
        {
            Text = GetModeName(mode),
            Disabled = component.Mode == mode,
            Priority = -(int) mode, // sort them in descending order
            Category = VerbCategory.SetSoundMode,
            Act = () => SetSoundMode(uid, mode, userUid, component)
        };
    }
    private string GetModeName(SpecialSoundMode mode)
    {
        string name;
        switch (mode)
        {
            case SpecialSoundMode.SpecialSoundOff:
                name = "verb-categories-special-sounds-mode-off";
                break;
            case SpecialSoundMode.SpecialSoundOn:
                name = "verb-categories-special-sounds-mode-on";
                break;
            default:
                return "";
        }

        return Loc.GetString(name);
    }
    public void SetSoundMode(EntityUid uid, SpecialSoundMode mode, EntityUid? userUid = null,
    SpecialSoundsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Mode = mode;

        if (userUid != null)
        {
            var msg = Loc.GetString("special-sounds-mode-state", ("mode", GetModeName(mode)));
            _popupSystem.PopupEntity(msg, uid, userUid.Value);

            switch (mode)
            {
                case SpecialSoundMode.SpecialSoundOff:
                    RaiseLocalEvent((EntityUid) userUid, new UnloadSpecialSoundsEvent(uid));
                    break;
                case SpecialSoundMode.SpecialSoundOn:
                    RaiseLocalEvent((EntityUid) userUid, new InitSpecialSoundsEvent(uid));
                    break;
                default:
                    return;
            }
        }
    }
}
