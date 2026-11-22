using Content.Shared.SS220.CCVars;
using Robust.Shared.Player;

namespace Content.Shared.SS220.TTS.Systems;

public partial class SharedTtsSystem
{
    public Action<ICommonSession>? OnSessionVoiceTestCooldownUpdated;

    private readonly Dictionary<ICommonSession, TimeSpan> _sessionVoiceTestCooldowns = [];

    private readonly HashSet<ICommonSession> _sessionsToRemoveBuffer = [];

    public TimeSpan DefaultVoiceTestRequestCooldown { get; private set; }

    private void InitializeVoiceTest()
    {
        _cfg.OnValueChanged(CCVars220.TtsVoiceTestRequestCooldown, v => DefaultVoiceTestRequestCooldown = TimeSpan.FromSeconds(v), true);
    }

    private void UpdateVoiceTest()
    {
        _sessionsToRemoveBuffer.Clear();

        var curTime = _timing.RealTime;
        foreach (var (session, endTime) in _sessionVoiceTestCooldowns)
        {
            if (curTime < endTime)
                continue;

            _sessionsToRemoveBuffer.Add(session);
        }

        foreach (var session in _sessionsToRemoveBuffer)
        {
            _sessionVoiceTestCooldowns.Remove(session);
            OnSessionVoiceTestCooldownUpdated?.Invoke(session);
        }
    }

    /// <summary>
    /// Whether the <paramref name="session"/> is currently on voice test cooldown.
    /// </summary>
    public bool IsVoiceTestCooldowned(ICommonSession session)
    {
        return _sessionVoiceTestCooldowns.ContainsKey(session);
    }

    public void AddVoiceTestCooldown(ICommonSession session, TimeSpan cooldown)
    {
        if (_sessionVoiceTestCooldowns.TryGetValue(session, out var existCooldown))
            cooldown += existCooldown;

        SetVoiceTestCooldown(session, cooldown);
    }

    public void RemoveVoiceTestCooldown(ICommonSession session, TimeSpan cooldown)
    {
        if (!_sessionVoiceTestCooldowns.TryGetValue(session, out var existCooldown))
            return;

        SetVoiceTestCooldown(session, existCooldown -= cooldown);
    }

    public void RemoveVoiceTestCooldown(ICommonSession session)
    {
        if (!_sessionVoiceTestCooldowns.ContainsKey(session))
            return;

        SetVoiceTestCooldown(session, TimeSpan.Zero);
    }

    public void SetVoiceTestCooldown(ICommonSession session, TimeSpan cooldown)
    {
        var curTime = _timing.RealTime;

        _sessionVoiceTestCooldowns[session] = curTime + cooldown;
        OnSessionVoiceTestCooldownUpdated?.Invoke(session);
    }

    public bool TryGetVoiceTestCooldown(ICommonSession session, out TimeSpan cooldown)
    {
        return _sessionVoiceTestCooldowns.TryGetValue(session, out cooldown);
    }
}
