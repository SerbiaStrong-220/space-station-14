using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.SS220.DarkForces.Narsi.Buildings.Forge.Receipts;
using Content.Shared.SS220.DarkForces.Narsi.Buildings;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using NarsiCultCraftReceiptCategoryPrototype = Content.Shared.SS220.DarkForces.Narsi.Craft.NarsiCultCraftReceiptCategoryPrototype;

namespace Content.Client.SS220.DarkForces.Narsi.Buildings.Forge;

[GenerateTypedNameReferences]
public sealed partial class NarsiForgeWindow : FancyWindow
{
    private readonly NarsiForgeBoundInterface _bui;
    public NarsiForgeWindow(NarsiForgeBoundInterface bui, EntityUid owner)
    {
        RobustXamlLoader.Load(this);
        _bui = bui;

        EntityView.SetEntity(owner);
    }

    public void UpdateState(NarsiForgeUIState state, List<NarsiCultCraftReceiptCategoryPrototype> prototypes)
    {
        RunicPlasteelCount.Text = $"{state.RunicPlasteelCount} штук";
        PlasteelCount.Text = $"{state.PlasteelCount} штук";
        SteelCount.Text = $"{state.SteelCount} штук";
        SetupForgeStatus(state.State);

        CTabContainer.RemoveAllChildren();
        PublishReceipts(prototypes, state.State == NarsiForgeState.Idle, state.RunicPlasteelCount, state.PlasteelCount, state.SteelCount);
    }

    private void SetupForgeStatus(NarsiForgeState state)
    {
        switch (state)
        {
            case NarsiForgeState.Idle:
                ForgeStatus.Text = "Готова к использованию";
                ForgeStatus.FontColorOverride = StyleNano.GoodGreenFore;
                break;
            case NarsiForgeState.Working:
                ForgeStatus.Text = "Используется";
                ForgeStatus.FontColorOverride = StyleNano.ConcerningOrangeFore;
                break;
            case NarsiForgeState.Delay:
                ForgeStatus.Text = "Остывает";
                ForgeStatus.FontColorOverride = StyleNano.DangerousRedFore;
                break;
        }
    }

    private void PublishReceipts(List<NarsiCultCraftReceiptCategoryPrototype> prototypes, bool isStateIdle, int runicPlasteelCount, int plasteelCount, int steelCount)
    {
        for (var i = 0; i < prototypes.Count; i++)
        {
            var prototype = prototypes[i];

            var category = new NarsiReceiptCategory(prototype, _bui, isStateIdle, runicPlasteelCount, plasteelCount, steelCount);
            CTabContainer.AddChild(category);
            CTabContainer.SetTabTitle(i, prototype.Title);
        }
    }
}