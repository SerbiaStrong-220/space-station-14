// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Forensics;
using Content.Server.Forensics.Components;
using Content.Server.SS220.Forensics.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.SS220.Forensics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Records individual glove contact traces independently of fingerprints and fibers.
/// </summary>
public sealed class GloveMicroFiberSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GloveMicroFiberComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InventoryComponent, ContactInteractionEvent>(OnContact);
    }

    private void OnMapInit(Entity<GloveMicroFiberComponent> ent, ref MapInitEvent args)
    {
        EnsureCode(ent.Comp);
    }

    private string EnsureCode(GloveMicroFiberComponent component)
    {
        if (component.Code != null)
            return component.Code;

        var bytes = new byte[16];
        _random.NextBytes(bytes);
        component.Code = Convert.ToHexString(bytes);
        return component.Code;
    }

    /// <summary>
    /// Obtains the permanent identity of gloves that leave identifiable contact traces.
    /// </summary>
    public bool TryGetCode(Entity<GloveMicroFiberComponent?> ent, out string? code)
    {
        code = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!ent.Comp.Enabled)
            return false;

        code = EnsureCode(ent.Comp);
        return true;
    }

    /// <summary>
    /// Obtains a glove's permanent identity and a snapshot of its prototype for contact evidence or a physical sample.
    /// </summary>
    public bool TryGetTrace(Entity<GloveMicroFiberComponent?> ent, out GloveMicroFiberTrace trace)
    {
        trace = default;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!TryGetCode(ent, out var code))
            return false;

        if (code == null)
            return false;

        var prototype = MetaData(ent).EntityPrototype;
        EntProtoId? prototypeId = prototype == null ? (EntProtoId?)null : new EntProtoId(prototype.ID);
        trace = new GloveMicroFiberTrace(code, prototypeId);
        return true;
    }

    private void OnContact(Entity<InventoryComponent> ent, ref ContactInteractionEvent args)
    {
        if (!HasComp<HandsComponent>(ent))
            return;

        if (HasComp<IgnoresFingerprintsComponent>(args.Other))
            return;

        if (!_inventory.TryGetSlotEntity(ent, "gloves", out var gloves))
            return;

        if (!TryGetTrace(gloves.Value, out var trace))
            return;

        EnsureComp<ForensicsComponent>(args.Other).GloveMicroFibers.Add(trace);
    }

    /// <summary>
    /// Retains the identity when cutting gloves into another pair of gloves.
    /// Does not turn cloth or other butchering products into sources of glove microfibers.
    /// </summary>
    public void TransferCode(Entity<GloveMicroFiberComponent?> source, Entity<GloveMicroFiberComponent?> target)
    {
        if (!Resolve(source, ref source.Comp, false))
            return;

        if (!Resolve(target, ref target.Comp, false))
            return;

        target.Comp.Code = EnsureCode(source.Comp);
    }
}
