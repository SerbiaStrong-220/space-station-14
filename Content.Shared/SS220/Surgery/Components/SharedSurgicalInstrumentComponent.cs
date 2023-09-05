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

        // Я хер его знает как щас сделать StepTime и InstrumentEfficiencyCoeff полезными, ибо doAfter-ы реализованы в прототипах рецептов

        public float StepTime { get; set; } // Время проведения шага

        [DataField("instrumentEfficiencyCoeff")]
        public float InstrumentEfficiencyCoeff { get; set; } // Отвечает за скорость работы инструмента >1 -> быстрее; <1 -> медленнее
        public EntityUid SelectedOrgan { get; set; }

    }
}
