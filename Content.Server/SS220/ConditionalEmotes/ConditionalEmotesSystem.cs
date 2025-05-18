// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Body.Components;
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
        if (!TryComp<TemperatureComponent>(ent, out var comp1) || !TryComp<ThermalRegulatorComponent>(ent, out var comp2))
            return;

        if (!ent.Comp.IsCold && args.CurrentTemperature < comp2.NormalBodyTemperature)
        {
            ent.Comp.IsCold = true;
            _autoEmote.AddEmote(ent.Owner, "LowTempShake");
        }
        else if (ent.Comp.IsCold && args.CurrentTemperature > comp2.NormalBodyTemperature)
        {
            ent.Comp.IsCold = false;
            _autoEmote.RemoveEmote(ent.Owner, "LowTempShake", null, false);
        }
        if (!ent.Comp.IsHot && args.CurrentTemperature > comp2.NormalBodyTemperature)
        {
            ent.Comp.IsHot = true;
            _autoEmote.AddEmote(ent.Owner, "HighTempSweat");
        }
        else if (ent.Comp.IsHot && args.CurrentTemperature < comp2.NormalBodyTemperature)
        {
            ent.Comp.IsHot = false;
            _autoEmote.RemoveEmote(ent.Owner, "HighTempSweat", null, false);
        }
    }
}
