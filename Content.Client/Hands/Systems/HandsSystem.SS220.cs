// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands.Components;

namespace Content.Client.Hands.Systems;

public sealed partial class HandsSystem
{
    private static void PrefixLayerKeys(List<(string, PrototypeLayerData)> layers, string handId)
    {
        for (var i = 0; i < layers.Count; i++)
        {
            var (key, data) = layers[i];
            layers[i] = ($"{handId}-{key}", data);
        }
    }
}

