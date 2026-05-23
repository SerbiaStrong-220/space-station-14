// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Globalization;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Administration.UI;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Sprite;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server.SS220.Administration.Systems;

public sealed class AdminVerbSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedScaleVisualsSystem _scaleVisuals = default!;

    private const string HealthChangeDamageType = "Cellular";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddAdminVerbs);
    }
    private void AddAdminVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Debug))
            return;

        if (HasComp<DamageableComponent>(args.Target))
        {
            Verb setLivesVerb = new()
            {
                Text = Loc.GetString("admin-verbs-lives-title"),
                Category = VerbCategory.Debug,
                // TODO need a proper icon here
                Act = () =>
                {
                    if (!_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Dead, out var deadThreshold))
                        return;

                    var maxHp = (int)deadThreshold.Value;

                    _quickDialog.OpenDialog(player,
                        Loc.GetString("admin-verbs-lives-title"),
                        Loc.GetString("admin-verbs-lives-health", ("maxHp", maxHp)),
                        (int desiredHp) =>
                        {
                            if (Deleted(args.Target)) return;

                            var targetHp = Math.Clamp(desiredHp, 0, maxHp);
                            var targetDamage = maxHp - targetHp;
                            var currentDamage = (int)_damageable.GetTotalDamage(args.Target);
                            var damageDelta = targetDamage - currentDamage;

                            if (damageDelta == 0) return;

                            var damageSpec = new DamageSpecifier();
                            damageSpec.DamageDict.Add(HealthChangeDamageType, damageDelta);
                            _damageable.TryChangeDamage(args.Target, damageSpec, true);
                        });
                }
            };
            args.Verbs.Add(setLivesVerb);
        }

        Verb traitsVerb = new()
        {
            Text = Loc.GetString("admin-verbs-traits-title"),
            Category = VerbCategory.Debug,
            // TODO need a proper icon here
            Act = () =>
            {
                var traitsEui = new ManageTraitsEui(args.Target, Name(args.Target));
                _euiManager.OpenEui(traitsEui, player);
            }
        };
        args.Verbs.Add(traitsVerb);

        Verb statusesVerb = new()
        {
            Text = Loc.GetString("admin-verbs-statuses-title"),
            Category = VerbCategory.Debug,
            // TODO need a proper icon here
            Act = () =>
            {
                var statusesEui = new ManageStatusesEui(args.Target, Name(args.Target));
                _euiManager.OpenEui(statusesEui, player);
            }
        };
        args.Verbs.Add(statusesVerb);

        Verb scaleVerb = new()
        {
            Text = Loc.GetString("admin-verbs-scale-title"),
            Category = VerbCategory.Debug,
            // TODO need a proper icon here
            Act = () =>
            {
                var curScale = _scaleVisuals.GetSpriteScale(args.Target);
                var curX = curScale.X.ToString("0.##");
                var curY = curScale.Y.ToString("0.##");

                _quickDialog.OpenDialog(player,
                    Loc.GetString("admin-verbs-scale-title"),
                    Loc.GetString("admin-verbs-scale-x-prompt", ("current", curX)),
                    Loc.GetString("admin-verbs-scale-y-prompt", ("current", curY)),
                    (string scaleXStr, string scaleYStr) =>
                    {
                        if (Deleted(args.Target))
                            return;

                        scaleXStr = scaleXStr.Replace(',', '.');
                        scaleYStr = scaleYStr.Replace(',', '.');

                        if (!float.TryParse(scaleXStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var scaleX) ||
                            !float.TryParse(scaleYStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var scaleY))
                        {
                            return;
                        }

                        if (scaleX <= 0f || scaleY <= 0f)
                            return;

                        var newScale = new System.Numerics.Vector2(scaleX, scaleY);
                        _scaleVisuals.SetSpriteScale(args.Target, newScale);
                    });
            }
        };
        args.Verbs.Add(scaleVerb);
    }
}
