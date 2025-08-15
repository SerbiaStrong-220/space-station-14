// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Examine;

namespace Content.Server.SS220.AutoEngrave;

public sealed class AutoEngravingSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AutoEngravingComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<AutoEngravingComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.AutoEngraveLocKey is null)
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.AutoEngraveLocKey, ("engraved", ent.Comp.EngravedText)));
    }
}
