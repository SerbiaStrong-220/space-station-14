// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    [Dependency] IChatManager _chatManager = default!;
    [Dependency] ChatSystem _chatSystem = default!;
    [Dependency] IPrototypeManager _prototypeManager = default!;
    private ProtoId<RadioChannelPrototype> _engineerRadio = "Engineering";
    private ProtoId<RadioChannelPrototype> _commonRadio = "Common";
    private char _engineerRadioKey = '\0';
    private char _commonRadioKey = '\0';


    private void InitializeAnnouncement()
    {
        _engineerRadioKey = _prototypeManager.Index(_engineerRadio).KeyCode;
        _commonRadioKey = _prototypeManager.Index(_commonRadio).KeyCode;
    }

    public void RadioAnnounceIntegrity(Entity<SuperMatterComponent> crystal, AnnounceIntegrityTypeEnum announceType)
    {
        if (announceType == AnnounceIntegrityTypeEnum.None)
            return;
        var integrity = GetIntegrity(crystal.Comp);
        var localePath = "supermatter-" + announceType.ToString().ToLower();
        var message = Loc.GetString(localePath, ("integrity", integrity));
        if (TryGetChannelKey(announceType, out var channelKey))
            message = channelKey + message;
        RadioAnnouncement(crystal.Owner, message);
    }
    public void StationAnnounceIntegrity(Entity<SuperMatterComponent> crystal, AnnounceIntegrityTypeEnum announceType)
    {
        if (!(announceType == AnnounceIntegrityTypeEnum.DelaminationStopped
            || announceType == AnnounceIntegrityTypeEnum.Delamination))
            return;
        var integrity = GetIntegrity(crystal.Comp);
        var localePath = "supermatter-" + announceType.ToString().ToLower();
        var message = Loc.GetString(localePath, ("integrity", integrity));
    }

    private void SendAdminChatAlert(Entity<SuperMatterComponent> crystal, string msg, string? whom = null)
    {
        var startString = $"SuperMatter {crystal} Alert! ";
        var endString = "";
        if (whom != null)
            endString = $" caused by {whom}.";
        _chatManager.SendAdminAlert(startString + msg + endString);
    }
    private void SendStationAnnouncement(EntityUid uid, string message, string? sender = null)
    {
        var localizedSender = GetLocalizedSender(sender);

        _chatSystem.DispatchStationAnnouncement(uid, message, localizedSender, colorOverride: Color.FromHex("#deb63d"));
        return;
    }
    private void RadioAnnouncement(EntityUid uid, string message)
    {
        _chatSystem.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, false, checkRadioPrefix: true);
    }
    private string GetLocalizedSender(string? sender)
    {
        var resultSender = sender ?? "supermatter-announcer";
        if (!Loc.TryGetString(resultSender, out var localizedSender))
            localizedSender = resultSender;
        return localizedSender;
    }
    /// <summary> Gets announce type, do it before zeroing AccumulatedDamage </summary>
    /// <param name="smComp"></param>
    /// <returns></returns>
    private AnnounceIntegrityTypeEnum GetAnnounceIntegrityType(SuperMatterComponent smComp)
    {
        var type = AnnounceIntegrityTypeEnum.Error;
        if (smComp.IntegrityDamageAccumulator > 0)
            type = smComp.Integrity switch
            {
                < 15f => AnnounceIntegrityTypeEnum.Delamination,
                < 35f => AnnounceIntegrityTypeEnum.Danger,
                < 80f => AnnounceIntegrityTypeEnum.Warning,
                _ => AnnounceIntegrityTypeEnum.None
            };
        else type = smComp.Integrity switch
        {
            < 15f => AnnounceIntegrityTypeEnum.DelaminationStopped,
            < 35f => AnnounceIntegrityTypeEnum.DangerRecovering,
            < 80f => AnnounceIntegrityTypeEnum.WarningRecovering,
            _ => AnnounceIntegrityTypeEnum.None
        };

        return type;
    }
    private bool TryGetChannelKey(AnnounceIntegrityTypeEnum announceType, [NotNullWhen(true)] out char? channelKey)
    {
        channelKey = announceType switch
        {
            AnnounceIntegrityTypeEnum.DangerRecovering => _commonRadioKey,
            AnnounceIntegrityTypeEnum.Danger => _commonRadioKey,
            AnnounceIntegrityTypeEnum.Delamination => _commonRadioKey,
            AnnounceIntegrityTypeEnum.DelaminationStopped => _commonRadioKey,
            AnnounceIntegrityTypeEnum.Warning => _engineerRadioKey,
            AnnounceIntegrityTypeEnum.WarningRecovering => _engineerRadioKey,
            _ => null
        };
        return channelKey is not null;
    }
}

public enum AnnounceIntegrityTypeEnum
{
    Error = -1,
    None,
    Warning,
    Danger,
    WarningRecovering,
    DangerRecovering,
    Delamination,
    DelaminationStopped
}
