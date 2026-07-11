// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.SS220.EyeOffsetInCombatMode;

public sealed class EyeOffsetInCombatModeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleOffset, InputCmdHandler.FromDelegate(OnOffsetToggle, handle: false, outsidePrediction: false))
            .Register<EyeOffsetInCombatModeSystem>();
    }

    private void OnOffsetToggle(ICommonSession? session)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } user)
            return;

        if (!TryComp<EyeOffsetInCombatModeComponent>(user, out var offsetComp))
            return;

        offsetComp.Online = !offsetComp.Online;

        Dirty<EyeOffsetInCombatModeComponent>((user, offsetComp));
    }

}
