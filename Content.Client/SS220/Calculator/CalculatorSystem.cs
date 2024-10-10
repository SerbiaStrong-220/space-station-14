// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.CCVar;
using Content.Shared.SS220.Calculator;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;

namespace Content.Client.SS220.Calculator;

public sealed class CalculatorSystem : SharedCalculatorSystem
{
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private float _interfaceSoundsGain;

    public override void Initialize()
    {
        base.Initialize();

        _configurationManager.OnValueChanged(CCVars.InterfaceVolume, OnInterfaceVolumeChanged, true);

        SubscribeNetworkEvent<CalculatorButtonPressedEvent>(OnButtonPressedNetworkEvent);
        SubscribeLocalEvent<CalculatorComponent, BoundUIClosedEvent>(OnUserInterfaceClosed);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _configurationManager.UnsubValueChanged(CCVars.InterfaceVolume, OnInterfaceVolumeChanged);
    }

    public void PlayButtonSound(Entity<CalculatorComponent> calculator, bool isUserInterface)
    {
        if (calculator.Comp.ButtonSound is not { } sound)
            return;
        // The current state of the audio system in the engine is terrifying.
        // I really hope all this volume stuff will be much easier someday. 
        var audioParams = sound.Params;
        if (isUserInterface)
        {
            audioParams = audioParams.AddVolume(SharedAudioSystem.GainToVolume(_interfaceSoundsGain));
        }
        _audioSystem.PlayEntity(sound, EntityUid.Invalid, calculator, audioParams);
    }

    protected override void OnChanged(Entity<CalculatorComponent> calculator)
    {
        // No automatic state sync
    }

    private void OnButtonPressedNetworkEvent(CalculatorButtonPressedEvent evt, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(evt.Calculator, out var calculatorEntityOrNone) || calculatorEntityOrNone is not { } calculatorEntity)
            return;
        if (!TryComp<CalculatorComponent>(calculatorEntity, out var calculator))
            return;

        PlayButtonSound((calculatorEntity, calculator), false);
    }

    private void OnUserInterfaceClosed(Entity<CalculatorComponent> calculator, ref BoundUIClosedEvent args)
    {
        RaiseNetworkEvent(new SetCalculatorStateMessage()
        {
            Calculator = GetNetEntity(calculator),
            State = calculator.Comp.State,
        });
    }

    private void OnInterfaceVolumeChanged(float value)
    {
        _interfaceSoundsGain = value;
    }
}
