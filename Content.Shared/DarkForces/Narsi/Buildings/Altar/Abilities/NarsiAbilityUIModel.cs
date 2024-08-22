﻿using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SecretStation.DarkForces.Narsi.Buildings.Altar.Abilities;

[Serializable, NetSerializable]
public record NarsiAbilityUIModel(string Id, string Name, string Description, string LevelDescription, int Level, int RequiredBloodScore, SpriteSpecifier Icon);
