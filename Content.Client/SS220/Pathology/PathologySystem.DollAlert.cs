// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Alerts;
using Content.Client.SS220.Surgery.SurgeryStartUi;
using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery;
using Content.Shared.SS220.Surgery.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Pathology;

/// <summary>
/// Tints the parts of the body doll alert by their traumas, the alert asks for it every frame.
/// </summary>
public sealed partial class PathologySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    // boxed once, the layer keys are looked up every frame
    private static readonly Enum[] DollLayerKeys = BoxDollLayerKeys();

    // scratch buffer refilled on every update: the sandbox forbids stackalloc and the update runs every frame
    private readonly PuppetZoneFlags[] _dollFlags = new PuppetZoneFlags[PuppetZones.Parts.Length];

    private static Enum[] BoxDollLayerKeys()
    {
        var keys = new Enum[PuppetZones.Parts.Length];
        for (var i = 0; i < keys.Length; i++)
        {
            keys[i] = PuppetZones.Parts[i];
        }

        return keys;
    }

    private void InitializeDollAlert()
    {
        SubscribeLocalEvent<PathologyHolderComponent, UpdateAlertSpriteEvent>(OnUpdateDollAlert);
    }

    private void OnUpdateDollAlert(Entity<PathologyHolderComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.DollAlert)
            return;

        TryComp<SurgeryPatientComponent>(ent, out var patient);
        var flags = _dollFlags.AsSpan();
        PuppetZones.FillFlags(flags, ent.Comp, patient, _prototype);

        var sprite = args.SpriteViewEnt.AsNullable();
        for (var i = 0; i < PuppetZones.Parts.Length; i++)
        {
            var part = PuppetZones.Parts[i];
            if (!_sprite.LayerMapTryGet(sprite, DollLayerKeys[i], out var layer, false))
                continue;

            // eyes and mouth are overlays over the head, seen only with a state of their own
            if (part is SurgeryPuppetPart.Eyes or SurgeryPuppetPart.Mouth)
            {
                var overlayTint = PuppetZones.ResolveTint(part, flags, false, null);
                _sprite.LayerSetVisible(sprite, layer, overlayTint != null);
                if (overlayTint != null)
                    _sprite.LayerSetColor(sprite, layer, overlayTint.Value);

                continue;
            }

            _sprite.LayerSetColor(sprite, layer, PuppetZones.ResolveTint(part, flags, false, PuppetZones.HealthyTint) ?? PuppetZones.HealthyTint);
        }
    }
}
