// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.DnaLock.Components;

/// <summary>
/// Компонент, добавляемый к одежде в рантайме, когда неавторизованный персонаж её надевает.
/// Хранит в себе информацию о том, кто её надел и когда должен произойти взрыв
/// </summary>
[RegisterComponent]
public sealed partial class DnaLockClothingActiveComponent : Component
{
    /// <summary>
    /// EntityUid персонажа, надевшего одежду без авторизации
    /// </summary>
    public EntityUid WearerUid;

    /// <summary>
    /// Игровое время, в которое должен произойти взрыв
    /// </summary>
    public TimeSpan ExplodeAt;

    /// <summary>
    /// Игровое время следующего бипа/попапа
    /// </summary>
    public TimeSpan NextBeepAt;
}

