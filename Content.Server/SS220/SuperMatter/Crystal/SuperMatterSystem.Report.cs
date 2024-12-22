// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Shared.Radio;
using Content.Shared.SS220.SuperMatter.Functions;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

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
        var localePath = GetLocalePath(announceType);
        var message = Loc.GetString(localePath, ("integrity", integrity.ToString("n2")));
        if (TryGetChannelKey(announceType, out var channelKey))
            message = $"{channelKey} {message}";
        RadioAnnouncement(crystal.Owner, message);
    }
    public void StationAnnounceIntegrity(Entity<SuperMatterComponent> crystal, AnnounceIntegrityTypeEnum announceType)
    {
        if (!(announceType == AnnounceIntegrityTypeEnum.DelaminationStopped
            || announceType == AnnounceIntegrityTypeEnum.Delamination))
            return;
        var integrity = GetIntegrity(crystal.Comp);
        var localePath = GetLocalePath(announceType);
        var message = Loc.GetString(localePath, ("integrity", integrity.ToString("n2")));
        SendStationAnnouncement(crystal.Owner, message);
    }
    public void StationAnnounceIntegrity(Entity<SuperMatterComponent> crystal, AnnounceIntegrityTypeEnum announceType, SuperMatterPhaseState smState)
    {
        if (!(announceType == AnnounceIntegrityTypeEnum.Explosion))
            Log.Warning("Used unexpecting announce type to announce Integrity with SuperMatterPhaseState ");

        var message = Loc.GetString(GetLocalePath(announceType, smState));
        SendStationAnnouncement(crystal.Owner, message);
    }

    private void SendAdminChatAlert(Entity<SuperMatterComponent> crystal, string msg, string? whom = null)
    {
        var stringBuilder = new StringBuilder($"SuperMatter {EntityManager.ToPrettyString(crystal)} Alert! ");
        stringBuilder.Append(msg);
        if (whom != null)
            stringBuilder.Append($" caused by {whom}.");
        _chatManager.SendAdminAlert(stringBuilder.ToString());
    }
    private void SendStationAnnouncement(EntityUid uid, string message, string? sender = null)
    {
        var localizedSender = GetLocalizedSender(sender);

        _chatSystem.DispatchStationAnnouncement(uid, message, localizedSender, colorOverride: Color.FromHex("#deb63d"));
        return;
    }
    private bool TryChangeStationAlertLevel(Entity<SuperMatterComponent> crystal, string alertLevel, [NotNullWhen(true)] out string? previousAlertLevel, bool force = true, bool locked = true)
    {
        previousAlertLevel = null;
        var stationUid = _station.GetStationInMap(Transform(crystal.Owner).MapID);
        if (!stationUid.HasValue)
            return false;

        previousAlertLevel = _alertLevel.GetLevel(stationUid.Value);

        if (crystal.Comp.UnchangeableAlertLevelList.Contains(previousAlertLevel))
            return false;

        _alertLevel.SetLevel(stationUid.Value, alertLevel, true, true, force, locked);
        return true;
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
    private bool TryGetChannelKey(AnnounceIntegrityTypeEnum announceType, [NotNullWhen(true)] out string? channelKey)
    {
        channelKey = announceType switch
        {
            AnnounceIntegrityTypeEnum.DangerRecovering => _commonRadioKey.ToString(),
            AnnounceIntegrityTypeEnum.Danger => _commonRadioKey.ToString(),
            AnnounceIntegrityTypeEnum.Delamination => _commonRadioKey.ToString(),
            AnnounceIntegrityTypeEnum.DelaminationStopped => _commonRadioKey.ToString(),
            AnnounceIntegrityTypeEnum.Warning => ":" + _engineerRadioKey.ToString(),
            AnnounceIntegrityTypeEnum.WarningRecovering => ":" + _engineerRadioKey.ToString(),
            _ => null
        };
        return channelKey is not null;
    }
    private string GetLocalePath(AnnounceIntegrityTypeEnum announceType, SuperMatterPhaseState? smState = null)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendJoin("-", ["supermatter", announceType.ToString().ToLower()]);

        if (smState.HasValue)
            stringBuilder.AppendJoin("-", ["", smState.Value.ToString().ToLower()]);

        return stringBuilder.ToString();
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
    DelaminationStopped,
    Explosion
}
