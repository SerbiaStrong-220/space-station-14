using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Shlepovend;

[RegisterComponent]
public sealed partial class ShlepovendComponent : Component
{
    /// <summary>
    ///     Sound that plays when ejecting an item
    /// </summary>
    [DataField("soundVend")]
    // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
    public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg")
    {
        Params = new AudioParams
        {
            Volume = -2f
        }
    };
}

[Serializable, NetSerializable]
public enum ShlepovendUiKey
{
    Key,
}
