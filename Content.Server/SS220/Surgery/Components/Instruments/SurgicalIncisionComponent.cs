using Content.Shared.SS220.Surgery;

namespace Content.Server.SS220.Surgery.Components.Instruments
{
    /// <summary>
    /// Используется для всех инструментов, которые могут выполнять роль скальпеля (расширение под гетто-хирургию)
    /// </summary>

    [RegisterComponent]
    public sealed partial class SurgicalIncisionComponent : SharedSurgicalInstrumentComponent
    {
        public byte? SelectedOperationMode { get; set; }
    }

}
