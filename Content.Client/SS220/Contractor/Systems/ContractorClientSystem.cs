using Content.Shared.Preferences;
using Content.Shared.SS220.Contractor;

namespace Content.Client.SS220.Contractor.Systems;

public sealed class ContractorClientSystem : SharedContractorSystem
{
    public Dictionary<EntityUid, HumanoidCharacterProfile> SpriteViewsForEntity = [];
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ContractorReceiveHumanoidMessage>(OnReceiveHumanoid);
    }

    private void OnReceiveHumanoid(ContractorReceiveHumanoidMessage msg)
    {
        SpriteViewsForEntity.Add(GetEntity(msg.Target), msg.Profile);
    }

}
