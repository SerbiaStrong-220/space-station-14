// using Content.Client.Animals.Components;
// using Content.Shared.Animals.Components;
// using Robust.Client.GameObjects;
//
// namespace Content.Client.Animals.Systems;
//
// public sealed class MouthContainerVisualizerSystem : VisualizerSystem<MouthContainerVisualsComponent>
// {
//     protected override void OnAppearanceChange(EntityUid uid, MouthContainerVisualsComponent comp, ref AppearanceChangeEvent args)
//     {
//         if (args.Sprite == null)
//             return;
//
//         if (AppearanceSystem.TryGetData<bool>(uid, MouthContainerVisuals.Stored, out var isStored, args.Component))
//             SpriteSystem.LayerSetVisible((uid, args.Sprite), MouthContainerVisualLayers.Cheeks, isStored);
//     }
// }
//
// public enum MouthContainerVisualLayers : byte
// {
//     Cheeks,
// }
