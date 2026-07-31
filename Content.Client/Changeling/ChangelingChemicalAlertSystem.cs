// SS220 Changeling
using Content.Client.Alerts;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Shared.Alert;
using Content.Shared.Alert.Components;
using Content.Shared.Changeling.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Changeling;

/// <summary>
/// Supplies the whole chemical units currently available to the changeling HUD counter.
/// </summary>
public sealed class ChangelingChemicalAlertSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly RSI.StateId[] ChemicalStates =
    [
        "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
        "10", "11", "12", "13", "14", "15", "16", "17", "18",
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingResourceComponent, GetGenericAlertCounterAmountEvent>(OnGetCounterAmount);
        SubscribeLocalEvent<ChangelingResourceComponent, UpdateAlertSpriteEvent>(OnUpdateAlertSprite);
    }

    private void OnGetCounterAmount(Entity<ChangelingResourceComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled || args.Alert != ent.Comp.ChemicalsAlert)
            return;

        args.Amount = ent.Comp.Chemicals.Int();
    }

    private void OnUpdateAlertSprite(Entity<ChangelingResourceComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.ChemicalsAlert || ent.Comp.MaxChemicals <= 0)
            return;

        var percentage = Math.Clamp((float) (ent.Comp.Chemicals.Float() / ent.Comp.MaxChemicals.Float()), 0f, 1f);
        var state = Math.Clamp((int) MathF.Round(percentage * (ChemicalStates.Length - 1)), 0, ChemicalStates.Length - 1);
        _sprite.LayerSetRsiState(args.SpriteViewEnt.AsNullable(), AlertVisualLayers.Base, ChemicalStates[state]);
    }
}
