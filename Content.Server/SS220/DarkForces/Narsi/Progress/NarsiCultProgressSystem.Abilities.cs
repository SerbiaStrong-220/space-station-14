using System.Linq;
using Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities.Prototype;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar.Abilities;

namespace Content.Server.SS220.DarkForces.Narsi.Progress;

public sealed partial class NarsiCultProgressSystem
{
    private static Dictionary<string, NarsiAbilityPrototype> _abilitiesPrototypes = new();

    private void InitializeAbilities()
    {
        _abilitiesPrototypes = _prototypeManager
            .EnumeratePrototypes<NarsiAbilityPrototype>()
            .ToDictionary(prototype => prototype.ID, prototype => prototype);
    }

    private List<NarsiAbilityPrototype> GetOpenedAbilities(NarsiCultProgressComponent component)
    {
        return component.OpenedAbilities.Select(openedAbility => _abilitiesPrototypes[openedAbility.Key]).ToList();
    }

    public NarsiAbilitiesState? GetAbilitiesUIState(EntityUid? user)
    {
        if (_activeProgress == null)
            return null;

        var component = _activeProgress.Value.Comp;
        var openedAbilities = GetOpenedAbilities(component);

        var openedUIState = new List<NarsiAbilityUIModel>();
        var closedUIState = new List<NarsiAbilityUIModel>();

        foreach (var openedAbility in openedAbilities)
        {
            var abilityLevel = component.OpenedAbilities[openedAbility.ID];
            var item = GetAbilityState(openedAbility, abilityLevel);
            openedUIState.Add(item);
        }

        foreach (var ability in _abilitiesPrototypes)
        {
            if (component.OpenedAbilities.TryGetValue(ability.Key, out var abilityLevel) && abilityLevel == 3)
                continue;

            var item = GetAbilityState(ability.Value, abilityLevel + 1);
            closedUIState.Add(item);
        }

        return new NarsiAbilitiesState(openedUIState, closedUIState, component.BloodPoints,
            HasComp<NarsiCultistLeaderComponent>(user));
    }

    private NarsiAbilityUIModel GetAbilityState(NarsiAbilityPrototype prototype, int abilityLevel)
    {
        var levelDescription = abilityLevel switch
        {
            1 => prototype.Level1Description,
            2 => prototype.Level2Description,
            3 => prototype.Level3Description,
            _ => prototype.Level1Description
        };

        return new NarsiAbilityUIModel(
            Id: prototype.ID,
            Name: prototype.Name,
            Description: prototype.Description,
            LevelDescription: levelDescription,
            Level: abilityLevel,
            RequiredBloodScore: prototype.RequiredBloodScore,
            Icon: prototype.Icon
        );
    }

    private bool TryIncreaseAbilityLevel(string id, int requiredBloodScore)
    {
        if (_activeProgress is not {} progress)
            return false;

        if (progress.Comp.OpenedAbilities[id] == 3)
            return false;

        progress.Comp.BloodPoints -= requiredBloodScore;
        progress.Comp.OpenedAbilities[id] += 1;

        SendMessageAboutAbility(progress, id, false);

        return true;
    }

    public bool TryOpenAbility(string id)
    {
        if (_activeProgress is not {} progress)
            return false;

        var requiredBloodScore = _abilitiesPrototypes[id].RequiredBloodScore;
        if (progress.Comp.BloodPoints < requiredBloodScore)
            return false;

        if (progress.Comp.OpenedAbilities.ContainsKey(id))
        {
            return TryIncreaseAbilityLevel(id, requiredBloodScore);
        }

        progress.Comp.OpenedAbilities[id] = 1;
        progress.Comp.BloodPoints -= requiredBloodScore;

        SendMessageAboutAbility(progress, id, true);

        return true;
    }

    private void SendMessageAboutAbility(Entity<NarsiCultProgressComponent> progress, string id, bool isOpened)
    {
        var abilityName = _abilitiesPrototypes[id].Name;
        if (isOpened)
        {
            SendMessageFromNarsi(progress,
                $"Открыта новая способность \"{abilityName}\"! Вырежите символ на теле, используя алтарь Нар'Си, чтобы использовать способность!");
            return;
        }

        var newLevel = progress.Comp.OpenedAbilities[id];
        SendMessageFromNarsi(progress, $"Способность \"{abilityName}\" была улучшена до {newLevel} уровня!");
    }

    public int GetAbilityLevel(string key)
    {
        if (_activeProgress == null)
            return 0;

        var component = _activeProgress.Value.Comp;
        return component.OpenedAbilities.TryGetValue(key, out var level) ? level : 0;
    }
}
