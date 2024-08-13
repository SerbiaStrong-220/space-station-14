// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.SuperMatterCrystal.Components;

namespace Content.Server.SS220.SuperMatterCrystal;
// TODO: Add PointLight variety depends on SM state or anything else
// TODO: Sprite changing should be made in client by subscribing the event
// look how done DamageStateVisualizerSystem. Also should make component and subscribe on DatabaseUpdateSystem
public sealed partial class SuperMatterSystem : EntitySystem
{
    private void UpdateSprite(Entity<SuperMatterComponent> entity)
    {

    }
}
