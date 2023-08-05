<<<<<<< HEAD
using System.Linq;
using Content.Shared.Decals;
=======
>>>>>>> off/master
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.Humanoid;

public sealed partial class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
{
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidAppearanceComponent, HumanoidMarkingModifierMarkingSetMessage>(OnMarkingsSet);
        SubscribeLocalEvent<HumanoidAppearanceComponent, HumanoidMarkingModifierBaseLayersSetMessage>(OnBaseLayersSet);
        SubscribeLocalEvent<HumanoidAppearanceComponent, GetVerbsEvent<Verb>>(OnVerbsRequest);
        SubscribeLocalEvent<HumanoidAppearanceComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, HumanoidAppearanceComponent component, ExaminedEvent args)
    {
        var identity = Identity.Entity(uid, EntityManager);
        var species = GetSpeciesRepresentation(component.Species).ToLower();
        var age = GetAgeRepresentation(component.Species, component.Age);

        args.PushText(Loc.GetString("humanoid-appearance-component-examine", ("user", identity), ("age", age), ("species", species)));
    }

<<<<<<< HEAD
    /// <summary>
    ///     Loads a humanoid character profile directly onto this humanoid mob.
    /// </summary>
    /// <param name="uid">The mob's entity UID.</param>
    /// <param name="profile">The character profile to load.</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void LoadProfile(EntityUid uid, HumanoidCharacterProfile profile, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        SetSpecies(uid, profile.Species, false, humanoid);
        SetSex(uid, profile.Sex, false, humanoid);
        humanoid.EyeColor = profile.Appearance.EyeColor;

        SetSkinColor(uid, profile.Appearance.SkinColor, false);

        humanoid.MarkingSet.Clear();

        // Add markings that doesn't need coloring. We store them until we add all other markings that doesn't need it.
        var markingFColored = new Dictionary<Marking, MarkingPrototype>();
        foreach (var marking in profile.Appearance.Markings)
        {
            if (_markingManager.TryGetMarking(marking, out var prototype))
            {
                if (!prototype.ForcedColoring)
                {
                    AddMarking(uid, marking.MarkingId, marking.MarkingColors, false);
                }
                else
                {
                    markingFColored.Add(marking, prototype);
                }
            }
        }

        // Hair/facial hair - this may eventually be deprecated.
        // We need to ensure hair before applying it or coloring can try depend on markings that can be invalid
        var hairColor = _markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.Hair, out var hairAlpha, _prototypeManager)
            ? profile.Appearance.SkinColor.WithAlpha(hairAlpha) : profile.Appearance.HairColor;
        var facialHairColor = _markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.FacialHair, out var facialHairAlpha, _prototypeManager)
            ? profile.Appearance.SkinColor.WithAlpha(facialHairAlpha) : profile.Appearance.FacialHairColor;
        
        if (_markingManager.Markings.TryGetValue(profile.Appearance.HairStyleId, out var hairPrototype) &&
            _markingManager.CanBeApplied(profile.Species, hairPrototype, _prototypeManager))
        {
            AddMarking(uid, profile.Appearance.HairStyleId, hairColor, false);
        }
        
        if (_markingManager.Markings.TryGetValue(profile.Appearance.FacialHairStyleId, out var facialHairPrototype) &&
            _markingManager.CanBeApplied(profile.Species, facialHairPrototype, _prototypeManager))
        {
            AddMarking(uid, profile.Appearance.FacialHairStyleId, facialHairColor, false); 
        }

        humanoid.MarkingSet.EnsureSpecies(profile.Species, profile.Appearance.SkinColor, _markingManager, _prototypeManager);

        // Finally adding marking with forced colors
        foreach (var (marking, prototype) in markingFColored)
        {
            var markingColors = MarkingColoring.GetMarkingLayerColors(
                prototype,
                profile.Appearance.SkinColor,
                profile.Appearance.EyeColor,
                humanoid.MarkingSet
            );
            AddMarking(uid, marking.MarkingId, markingColors, false);
        }
        
        EnsureDefaultMarkings(uid, humanoid);
        SetTTSVoice(uid, profile.Voice, humanoid); // Corvax-TTS

        humanoid.Gender = profile.Gender;
        if (TryComp<GrammarComponent>(uid, out var grammar))
        {
            grammar.Gender = profile.Gender;
        }

        humanoid.Age = profile.Age;
        // Corvax-SpeakerColor-Start
        const string paletteId = "Material";
        var colors = _prototypeManager.Index<ColorPalettePrototype>(paletteId).Colors.Values.ToArray();
        var colorIdx = Math.Abs(profile.Name.GetHashCode() % colors.Length);
        humanoid.SpeakerColor = colors[colorIdx];
        // Corvax-SpeakerColor-End

        Dirty(humanoid);
    }

=======
>>>>>>> off/master
    // this was done enough times that it only made sense to do it here

    /// <summary>
    ///     Clones a humanoid's appearance to a target mob, provided they both have humanoid components.
    /// </summary>
    /// <param name="source">Source entity to fetch the original appearance from.</param>
    /// <param name="target">Target entity to apply the source entity's appearance to.</param>
    /// <param name="sourceHumanoid">Source entity's humanoid component.</param>
    /// <param name="targetHumanoid">Target entity's humanoid component.</param>
    public void CloneAppearance(EntityUid source, EntityUid target, HumanoidAppearanceComponent? sourceHumanoid = null,
        HumanoidAppearanceComponent? targetHumanoid = null)
    {
        if (!Resolve(source, ref sourceHumanoid) || !Resolve(target, ref targetHumanoid))
        {
            return;
        }

        targetHumanoid.Species = sourceHumanoid.Species;
        targetHumanoid.SkinColor = sourceHumanoid.SkinColor;
        targetHumanoid.EyeColor = sourceHumanoid.EyeColor;
        targetHumanoid.Age = sourceHumanoid.Age;
        SetSex(target, sourceHumanoid.Sex, false, targetHumanoid);
        targetHumanoid.CustomBaseLayers = new(sourceHumanoid.CustomBaseLayers);
        targetHumanoid.MarkingSet = new(sourceHumanoid.MarkingSet);
        SetTTSVoice(target, sourceHumanoid.Voice, targetHumanoid); // Corvax-TTS

        targetHumanoid.Gender = sourceHumanoid.Gender;
        if (TryComp<GrammarComponent>(target, out var grammar))
        {
            grammar.Gender = sourceHumanoid.Gender;
        }

        Dirty(targetHumanoid);
    }

    /// <summary>
    ///     Removes a marking from a humanoid by ID.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="marking">The marking to try and remove.</param>
    /// <param name="sync">Whether to immediately sync this to the humanoid</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void RemoveMarking(EntityUid uid, string marking, bool sync = true, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        humanoid.MarkingSet.Remove(prototype.MarkingCategory, marking);

        if (sync)
            Dirty(humanoid);
    }

    /// <summary>
    ///     Removes a marking from a humanoid by category and index.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="category">Category of the marking</param>
    /// <param name="index">Index of the marking</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void RemoveMarking(EntityUid uid, MarkingCategories category, int index, HumanoidAppearanceComponent? humanoid = null)
    {
        if (index < 0
            || !Resolve(uid, ref humanoid)
            || !humanoid.MarkingSet.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        humanoid.MarkingSet.Remove(category, index);
        Dirty(humanoid);
    }

    /// <summary>
    ///     Sets the marking ID of the humanoid in a category at an index in the category's list.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="category">Category of the marking</param>
    /// <param name="index">Index of the marking</param>
    /// <param name="markingId">The marking ID to use</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetMarkingId(EntityUid uid, MarkingCategories category, int index, string markingId, HumanoidAppearanceComponent? humanoid = null)
    {
        if (index < 0
            || !_markingManager.MarkingsByCategory(category).TryGetValue(markingId, out var markingPrototype)
            || !Resolve(uid, ref humanoid)
            || !humanoid.MarkingSet.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        var marking = markingPrototype.AsMarking();
        for (var i = 0; i < marking.MarkingColors.Count && i < markings[index].MarkingColors.Count; i++)
        {
            marking.SetColor(i, markings[index].MarkingColors[i]);
        }

        humanoid.MarkingSet.Replace(category, index, marking);
        Dirty(humanoid);
    }

    /// <summary>
    ///     Sets the marking colors of the humanoid in a category at an index in the category's list.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="category">Category of the marking</param>
    /// <param name="index">Index of the marking</param>
    /// <param name="colors">The marking colors to use</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetMarkingColor(EntityUid uid, MarkingCategories category, int index, List<Color> colors,
        HumanoidAppearanceComponent? humanoid = null)
    {
        if (index < 0
            || !Resolve(uid, ref humanoid)
            || !humanoid.MarkingSet.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        for (var i = 0; i < markings[index].MarkingColors.Count && i < colors.Count; i++)
        {
            markings[index].SetColor(i, colors[i]);
        }

        Dirty(humanoid);
    }

    /// <summary>
    /// Takes ID of the species prototype, returns UI-friendly name of the species.
    /// </summary>
    public string GetSpeciesRepresentation(string speciesId)
    {
        if (_prototypeManager.TryIndex<SpeciesPrototype>(speciesId, out var species))
        {
            return Loc.GetString(species.Name);
        }
        else
        {
            return Loc.GetString("humanoid-appearance-component-unknown-species");
        }
    }

    public string GetAgeRepresentation(string species, int age)
    {
        _prototypeManager.TryIndex<SpeciesPrototype>(species, out var speciesPrototype);

        if (speciesPrototype == null)
        {
            Logger.Error("Tried to get age representation of species that couldn't be indexed: " + species);
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
        {
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.OldAge)
        {
            return Loc.GetString("identity-age-middle-aged");
        }

        return Loc.GetString("identity-age-old");
    }
}
