// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.DnaLock.Components;

/// <summary>
/// Компонент для одежды с ДНК-локом: если неавторизованный персонаж надевает предмет,
/// запускается таймер с бипом и попапом. По истечению - взрыв и удаление предмета.
/// Требует наличия DnaLockableComponent на том же объекте.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DnaLockClothingComponent : Component
{
    /// <summary>
    /// Время до взрыва после надевания неавторизованным пользователем
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TimeToExplode = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Интервал между сигналами бипера
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BeepInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Звук предупреждения (бипа)
    /// </summary>
    [DataField]
    public SoundSpecifier WarningSound = new SoundPathSpecifier("/Audio/Items/beep.ogg");

    /// <summary>
    /// Попап, видимый всем в зоне видимости
    /// </summary>
    [DataField]
    public string WarningPopupOthers = "dna-lock-clothing-unauthorized-warning";

    /// <summary>
    /// Попап для самого нарушителя
    /// </summary>
    [DataField]
    public string WarningPopupWearer = "dna-lock-clothing-unauthorized-warning-self";

    /// <summary>
    /// Попап обратного отсчета для окружающих
    /// </summary>
    [DataField]
    public string TimerPopupOthers = "dna-lock-clothing-timer-warning";

    /// <summary>
    /// Попап обратного отсчета для самого нарушителя
    /// </summary>
    [DataField]
    public string TimerPopupWearer = "dna-lock-clothing-timer-warning-self";

    [DataField]
    public string ExplosionType = "Default";

    /// <summary>
    /// Интенсивность взрыва. Достаточно для крита, но не смерти <= так задумано
    /// </summary>
    [DataField]
    public float ExplosionTotalIntensity = 10f;

    /// <summary>
    /// Затухание взрыва
    /// </summary>
    [DataField]
    public float ExplosionSlope = 100f;

    [DataField]
    public float ExplosionMaxTileIntensity = 10f;
}
