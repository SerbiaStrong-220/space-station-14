// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Server.Temperature.Components;
using Content.Shared.Temperature;

namespace Content.Server.SS220.ConditionalEmotes;

public sealed class ConditionalEmotesSystem : EntitySystem
{
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConditionalEmotesComponent, OnTemperatureChangeEvent>(OnTemperatureChanged);
    }

    private void OnTemperatureChanged(Entity<ConditionalEmotesComponent> ent, ref OnTemperatureChangeEvent args)
    {
        if (!TryComp<TemperatureComponent>(ent, out var comp))
            return;
        Log.Info($"EVENT TRIGGERED WITH TEMP OF ENTITY: {args.CurrentTemperature} AND TEMP COLD STATUS: ${ent.Comp.IsCold}");

        if (!ent.Comp.IsCold && args.CurrentTemperature < comp.ColdDamageThreshold)
        {
            ent.Comp.IsCold = true;
            Log.Info("temp is lower, sucking");
            _autoEmote.AddEmote(ent.Owner, "LowTempShake");
            Log.Info($"TEMP EVENT (ADD): temp={args.CurrentTemperature}, threshold={comp.ColdDamageThreshold}, isCold={ent.Comp.IsCold}");
        }
        else if (ent.Comp.IsCold && args.CurrentTemperature > comp.ColdDamageThreshold)
        {
            ent.Comp.IsCold = false;
            Log.Info("temp is higher, deleting");
            _autoEmote.RemoveEmote(ent.Owner, "LowTempShake");
            Log.Info($"TEMP EVENT (REMOVE): temp={args.CurrentTemperature}, threshold={comp.ColdDamageThreshold}, isCold={ent.Comp.IsCold}");
        }
    }
}
