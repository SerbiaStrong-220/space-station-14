using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.PDA;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class CrewManifestUi : UIFragment
{
    private CrewManifestUiFragment? _fragment;

    // ss220 add additional info for pda start
    private Entity<PdaComponent>? _pda;
    private static readonly ProtoId<TagPrototype> LinkRecordsTag = "LinkRecordsPda";
    // ss220 add additional info for pda end

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        // ss220 add additional info for pda start
        var entMan = IoCManager.Resolve<IEntityManager>();
        var tagSys = entMan.System<TagSystem>();

        if (fragmentOwner != null &&
            entMan.TryGetComponent<CartridgeComponent>(fragmentOwner, out var cartridge) &&
            cartridge.LoaderUid != null && entMan.TryGetComponent<PdaComponent>(cartridge.LoaderUid.Value, out var pda) &&
            tagSys.HasTag(cartridge.LoaderUid.Value, LinkRecordsTag))
        {
            _pda = (cartridge.LoaderUid.Value, pda);
        }
        // ss220 add additional info for pda end

        _fragment = new CrewManifestUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CrewManifestUiState crewManifestState)
            return;

        _fragment?.UpdateState(crewManifestState.StationName, crewManifestState.Entries, _pda); // ss220 add additional info for pda
    }
}
