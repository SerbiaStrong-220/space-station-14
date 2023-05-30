using Robust.Client.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Client.Administration.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Eye.SecurityHud.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Administration;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.Eye.SecurityHud
{
    public sealed class SecurityHudOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] IEntityManager _entityManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        private readonly IEyeManager _eyeManager;
        private readonly EntityLookupSystem _entityLookup;
        private readonly Font _font;
        [Dependency] ILightManager _lightManager = default!;
        private InventorySystem _inventorySystem;

        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

        private HudableComponent _hudableComponent = default!;

        public SecurityHudOverlay(IEntityManager entityManager, IEyeManager eyeManager,
            IResourceCache resourceCache, EntityLookupSystem entityLookup)
        {
            _inventorySystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InventorySystem>();
            IoCManager.InjectDependencies(this);
            _entityLookup = entityLookup;
            _entityManager = entityManager;
            _eyeManager = eyeManager;
            _font = new VectorFont(resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
        }
        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (!_entityManager.TryGetComponent(_player.LocalPlayer?.ControlledEntity, out EyeComponent? eyeComp))
                return false;

            if (args.Viewport.Eye != eyeComp.Eye)
                return false;
            var playerEntity = _player.LocalPlayer?.ControlledEntity;

            if (playerEntity == null)
                return false;

            if (!_entityManager.TryGetComponent<HudableComponent>(playerEntity, out var hudableComp))
                return false;

            _hudableComponent = hudableComp;

            var visible = _hudableComponent.IsVisible;

            if (!visible && _hudableComponent.LightSetup) // Turn FOV back on if we can see again
            {
                _lightManager.Enabled = true;
                return true;
            }

            return visible;
        }
        protected override void Draw(in OverlayDrawArgs args)
        {
            var viewport = args.WorldAABB;
            var nearbyPlayers = _entityManager.GetAllComponents(typeof(IdentityComponent)).ToList();
            foreach (var playerInfo in nearbyPlayers)
            {
                // Otherwise the entity can not exist yet
                if (!_entityManager.EntityExists(playerInfo.Owner))
                {
                    continue;
                }
                var entity = playerInfo.Owner;
                //var access = _entityManager.GetComponent<AccessComponent>(entity);
                var test = _entityManager.GetAllComponents(typeof(IdCardComponent)).ToList();
                var test2 = _inventorySystem.TryGetSlotEntity(entity, "id", out var test3);
                if (test3 != null)
                {
                    var test4 = _entityManager.GetComponent<MetaDataComponent>(test3.Value).EntityPrototype?.ID;
                    test4 = test4?.Replace("PDA", "").Replace("IDCard", "");
                    // if not on the same map, continue
                    var success =
                        _resourceCache.TryGetResource<TextureResource>(
                            new ResPath($"/Textures/Interface/JobIcons/{test4}.png"), out var texture);

                    if (_entityManager.GetComponent<TransformComponent>(entity).MapID != _eyeManager.CurrentMap)
                    {
                        continue;
                    }

                    var aabb = _entityLookup.GetWorldAABB(entity);

                    // if not on screen, continue
                    if (!aabb.Intersects(in viewport))
                    {
                        continue;
                    }

                    
                    if (texture != null)
                        args.WorldHandle.DrawTexture(texture, aabb.Center + new Angle(-_eyeManager.CurrentEye.Rotation)
                            .RotateVec(
                                aabb.TopRight - aabb.Center));
                }
            }
        }

        private string FixRoleName(string roleName)
        {
            switch (roleName)
            {
                case "CBURN":
                    return "Nanotrasen";
                case "Cluwne":
                    return "Clown";
                case "ERTLeader":
                    return "Nanotrasen";
                case "Syndi":
                    return "Nold";
                case "Syndicate":
                    return "Nold";
                //case "CBURN":
                //    return "Centcom";
                //case "CBURN":
                //    return "Centcom";
                //case "CBURN":
                //    return "Centcom";
                //case "CBURN":
                //    return "Centcom";
                //case "CBURN":
                //    return "Centcom";
                default:
                    return roleName;
            }
        }
    }
}
