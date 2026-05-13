// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Speech.EntitySystems;
using Content.Server.SS220.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.SS220.Speech.EntitySystems;

public sealed class ItalianAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItalianAccentComponent, AccentGetEvent>(OnAccent);
    }

    private string Accentuate(string message)
    {
        return _replacement.ApplyReplacements(message, "italian");
    }

    private void OnAccent(Entity<ItalianAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
