using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class AntiGravityClothingSystem : EntitySystem
{
    /// <inheritdoc/>
    [Dependency] private readonly SharedJetpackSystem _jetpackSystem = default!; //SS220 Moonboots with jet fix
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!; //SS220 Moonboots with jet fix

    public override void Initialize()
    {
        SubscribeLocalEvent<AntiGravityClothingComponent, InventoryRelayedEvent<IsWeightlessEvent>>(OnIsWeightless);
        SubscribeLocalEvent<AntiGravityClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped); //SS220 Moonboots with jet fix
    }

    private void OnIsWeightless(Entity<AntiGravityClothingComponent> ent, ref InventoryRelayedEvent<IsWeightlessEvent> args)
    {
        if (args.Args.Handled)
            return;

        args.Args.Handled = true;
        args.Args.IsWeightless = true;
    }

    //SS220 Moonboots with jet fix begin
    private void OnUnequipped(Entity<AntiGravityClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (TryComp<JetpackUserComponent>(args.Wearer, out var jetpackUserComp) &&
            TryComp<JetpackComponent>(jetpackUserComp.Jetpack, out var jetpack))
        {
            _jetpackSystem.SetEnabled(jetpackUserComp.Jetpack, jetpack, false, args.Wearer);
            _popupSystem.PopupClient(Loc.GetString("jetpack-to-grid"), jetpackUserComp.Jetpack, args.Wearer);
        }
    }
    //SS220 Moonboots with jet fix end
}
