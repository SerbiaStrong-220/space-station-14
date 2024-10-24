// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.SS220.Photography;
using Content.Shared.Verbs;
using Robust.Server.Audio;

namespace Content.Server.SS220.Photography;

public sealed class PhotoCameraSystem : EntitySystem
{
    [Dependency] private readonly PhotoManager _photo = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhotoCameraComponent, UseInHandEvent>(OnCameraActivate);
        SubscribeLocalEvent<PhotoCameraComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<PhotoCameraComponent, ExaminedEvent>(OnCameraExamine);
        SubscribeLocalEvent<PhotoComponent, ExaminedEvent>(OnPhotoExamine);
        SubscribeLocalEvent<PhotoFilmComponent, AfterInteractEvent>(OnFilmUse);
        SubscribeLocalEvent<PhotoFilmComponent, ExaminedEvent>(OnFilmExamine);
    }

    private void OnPhotoExamine(Entity<PhotoComponent> entity, ref ExaminedEvent args)
    {
        if (entity.Comp.SeenObjects.Count == 0)
            return;

        var seenFormatted = string.Join(", ", entity.Comp.SeenObjects.ToArray());
        args.PushMarkup(Loc.GetString("photo-component-seen-objects", ("seen", seenFormatted)));
    }

    private void OnVerb(Entity<PhotoCameraComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        // standard interaction checks
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        foreach (var size in entity.Comp.AvailablePhotoDimensions)
        {
            var verb = new Verb()
            {
                Text = GetPhotoSizeLoc(size),
                Disabled = entity.Comp.SelectedPhotoDimensions == size,
                Priority = (int) size, // sort them in ascending order
                Category = VerbCategory.PhotoSize,
                Act = () => SetPhotoSize(entity, entity.Comp, size)
            };

            args.Verbs.Add(verb);
        }
    }

    public void SetPhotoSize(EntityUid entity, PhotoCameraComponent component, float size)
    {
        component.SelectedPhotoDimensions = size;
        Dirty(entity, component);
    }

    private void OnCameraExamine(Entity<PhotoCameraComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(
            "photo-film-component-selected-photo-size",
            ("size", GetPhotoSizeLoc(entity.Comp.SelectedPhotoDimensions))
        ));
    }

    private void OnFilmExamine(Entity<PhotoFilmComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("photo-film-component-charges", ("charges", entity.Comp.Charges)));
    }

    private void OnFilmUse(Entity<PhotoFilmComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target is not { Valid: true } target || !HasComp<PhotoCameraComponent>(target))
            return;

        _charges.AddCharges(target, entity.Comp.Charges);
        EntityManager.DeleteEntity(entity);
    }

    private void OnCameraActivate(Entity<PhotoCameraComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        TryComp<LimitedChargesComponent>(entity, out var charges);
        if (_charges.IsEmpty(entity, charges))
        {
            _popup.PopupEntity(Loc.GetString("photo-camera-component-no-charges-message"), entity, args.User, PopupType.Medium);
            return;
        }

        if (!TryPhoto(entity, out var photo))
            return;

        if (TryComp<HandsComponent>(args.User, out var hands))
        {
            if (_hands.TryGetEmptyHand(args.User, out var emptyHand, hands))
                _hands.TryPickup(args.User, photo.Value, emptyHand, checkActionBlocker: false, handsComp: hands);
        }

        _charges.UseCharge(entity, charges);
        _audio.PlayPvs(entity.Comp.ShotSound, entity);
        Dirty(entity);
        args.Handled = true;
    }

    private string GetPhotoSizeLoc(float size)
    {
        return Loc.GetString("photo-camera-component-photo-size", ("size", size));
    }

    private bool TryPhoto(Entity<PhotoCameraComponent> entity, [NotNullWhen(true)] out EntityUid? photoEntity)
    {
        photoEntity = null;

        if (entity.Comp.FilmLeft <= 0)
            return false;

        if (!TryComp<TransformComponent>(entity, out var xform))
            return false;

        Angle cameraRotation;

        if (xform.GridUid is { } gridToAllignWith)
            cameraRotation = _transform.GetWorldRotation(gridToAllignWith);
        else
            cameraRotation = _transform.GetWorldRotation(xform);

        if (!_photo.TryCapture(xform.MapPosition, cameraRotation, entity.Comp.SelectedPhotoDimensions, out var id, out var seenObjects))
            return false;

        photoEntity = Spawn(entity.Comp.PhotoPrototypeId, xform.MapPosition);
        var photoComp = EnsureComp<PhotoComponent>(photoEntity.Value);
        photoComp.PhotoID = id;
        photoComp.SeenObjects = seenObjects;
        Dirty(photoEntity.Value, photoComp);

        return true;
    }
}
