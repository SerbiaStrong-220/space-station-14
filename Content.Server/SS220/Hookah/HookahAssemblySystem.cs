using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.Hookah.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Hookah;

public sealed class HookahAssemblySystem : EntitySystem
{
    private static readonly EntProtoId HookahPartialId = "HookahPartial";
    private static readonly EntProtoId HookahPartialFullId = "HookahPartialFull";
    private static readonly EntProtoId HookahBaseId = "HookahBase";
    private static readonly LocId HookahAssemblyStage1 = "hookah-assembly-stage1";
    private static readonly LocId HookahAssemblyStage2 = "hookah-assembly-stage2";
    private static readonly LocId HookahAssemblyComplete = "hookah-assembly-complete";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HookahFlaskComponent, InteractUsingEvent>(OnFlaskInteract);
        SubscribeLocalEvent<HookahPartialComponent, InteractUsingEvent>(OnPartialInteract);
        SubscribeLocalEvent<HookahPartialFullComponent, InteractUsingEvent>(OnPartialFullInteract);
    }

    private void OnFlaskInteract(Entity<HookahFlaskComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<HookahShaftComponent>(args.Used))
            return;

        Assemble(ent, args.Used, HookahPartialId, ent.Comp.AssemblySound);
        _popup.PopupEntity(Loc.GetString(HookahAssemblyStage1), args.User, args.User);
        args.Handled = true;
    }

    private void OnPartialInteract(Entity<HookahPartialComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<HookahSaucerComponent>(args.Used))
            return;

        Assemble(ent, args.Used, HookahPartialFullId, ent.Comp.AssemblySound);
        _popup.PopupEntity(Loc.GetString(HookahAssemblyStage2), args.User, args.User);
        args.Handled = true;
    }

    private void OnPartialFullInteract(Entity<HookahPartialFullComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<HookahTubePartComponent>(args.Used))
            return;

        Assemble(ent, args.Used, HookahBaseId, ent.Comp.AssemblySound);
        _popup.PopupEntity(Loc.GetString(HookahAssemblyComplete), args.User, args.User);
        args.Handled = true;
    }

    private void Assemble(EntityUid target, EntityUid used, EntProtoId resultProto, SoundSpecifier sound)
    {
        var coords = Transform(target).Coordinates;
        var result = Spawn(resultProto, coords);
        _audio.PlayPvs(sound, result);
        QueueDel(target);
        QueueDel(used);
    }
}
