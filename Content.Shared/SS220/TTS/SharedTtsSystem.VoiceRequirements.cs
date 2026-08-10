using Content.Shared.Preferences;
using Content.Shared.SS220.TTS.Requirements;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.TTS;

public partial class SharedTtsSystem
{
    public bool IsPassVoiceRequirements(TtsVoicePrototype proto, TtsVoiceRequirementCheckData data, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        if (proto.Requirement == null)
            return true;

        if (!data.IsRequirementAllowed(proto.Requirement))
            return true;

        return proto.Requirement.Check(EntityManager, data, out reason);
    }
}

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class TtsVoiceRequirement
{
    [DataField]
    public bool Inverted = false;

    public abstract bool Check(IEntityManager entityManager, TtsVoiceRequirementCheckData data, [NotNullWhen(false)] out FormattedMessage? reason);
}


public struct TtsVoiceRequirementCheckData
{
    public ICommonSession? Session;
    public HumanoidCharacterProfile? Profile;
    public EntityUid? Entity;

    public Type[]? AllowedRequirements;
    public Type[]? NotAllowedRequrements;

    public readonly bool IsRequirementAllowed(TtsVoiceRequirement requirement)
    {
        return IsRequirementAllowed(requirement.GetType());
    }

    public readonly bool IsRequirementAllowed(Type type)
    {
        DebugTools.Assert(type.IsAssignableTo(typeof(TtsVoiceRequirement)));

        if (AllowedRequirements?.Contains(type) == false)
            return false;

        if (NotAllowedRequrements?.Contains(type) == true)
            return false;

        return true;
    }
}
