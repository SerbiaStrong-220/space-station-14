// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Medical;
using Content.Shared.SS220.Experience.DoAfterEffect.Components;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Systems;

public sealed class MedicineDoAfterSkillSystem : BaseDoAfterSkillSystem<MedicineDoAfterSkillComponent, HealingDoAfterEvent>;
