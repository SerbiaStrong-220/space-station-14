using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery
{
    [NetSerializable()]
    public abstract partial class SharedSurgicalInstrumentComponent : Component
    {
        /// <summary>
        ///  У каждого инструмента есть шанс удачного выполнения своего шага
        ///  Для хирургических инструментов он всегда 100%
        ///  Для гетто-хирургии есть шансы провала шага, наносящие урон человеку
        /// </summary>

        [DataField("succesfulStepChance")]

        public float SuccesfulStepChance = 100f;

        /// <summary>
        /// Инструмент может быть загрязнён, что влияет на шанс нанесения инфекции на органы или конечность (пока не нужно)
        /// </summary>

        public bool IsInfected = false;

    }
}
