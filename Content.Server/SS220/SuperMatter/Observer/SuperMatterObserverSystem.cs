// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SuperMatter.Ui;

namespace Content.Server.SS220.SuperMatter.Observer;

public sealed class SuperMatterObserverSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        // To think maybe i dont need to broadcast it for clients,
        // but still we will have panels with this info
        // think of splitting, but does it cost it?
        SubscribeNetworkEvent<SuperMatterStateUpdate>(OnCrystalUpdate);
    }

    private void OnCrystalUpdate(SuperMatterStateUpdate args)
    {

    }
}
