// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Speech.EntitySystems;
using Content.Server.SS220.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.SS220.Speech.EntitySystems;

public sealed class DwarfAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DwarfAccentComponent, AccentGetEvent>(OnAccent);
    }

    private string Accentuate(string message)
    {
        return _replacement.ApplyReplacements(message, "dwarf");
    }

    private void OnAccent(Entity<DwarfAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
