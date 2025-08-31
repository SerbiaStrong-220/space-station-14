// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

// THE only reason if DoAfter living in on folder and namespace is it "WA DA IS IT" nature
namespace Content.Shared.SS220.Experience.DoAfterEffect;

public sealed partial class ExperienceSystem : EntitySystem
{
    // public bool OverrideNotNullValues = false;
    // public bool OverrideNullValues = true;
    // public bool ApplyIfAlreadyHave = true;
    public void Merge(Entity<SkillDoAfterEffectContainerComponent?> entity, ref readonly SkillDoAfterEffectComponent skillDoAfterEffect,
                                                                  bool applyIfAlreadyHave, bool overrideNullValues, bool overrideNotNullValues)
    {
        entity.Comp = EnsureComp<SkillDoAfterEffectContainerComponent>(entity.Owner);

        foreach (var (ev, effectInfo) in skillDoAfterEffect.Effect)
        {
            var evGuid = ev.GetType().GUID;
            if (entity.Comp.EffectContainer.TryGetValue(evGuid, out var effect))
            {
                MergeDoAfterEffects(effect, in effectInfo, overrideNullValues, overrideNotNullValues);
                entity.Comp.EffectContainer[evGuid] = effect;
            }
            else
            {
                entity.Comp.EffectContainer.Add(evGuid, effectInfo);
            }
        }
    }

    private void MergeDoAfterEffects(DoAfterEffect effect, ref readonly DoAfterEffect skillEffect, bool overrideNullValues, bool overrideNotNullValues)
    {

    }
}
