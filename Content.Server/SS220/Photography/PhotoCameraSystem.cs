// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.Photography;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Shared.Map;

namespace Content.Server.SS220.Photography;

public sealed partial class PhotoCameraSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PhotoManager _photo = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhotoCameraComponent, BeforeRangedInteractEvent>(OnCameraInteract);
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
        Del(entity);
    }

    private void OnCameraInteract(Entity<PhotoCameraComponent> entity, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled)
            return;

        // The camera owns this click even if the focus is invalid or the film is empty.
        args.Handled = true;
        if (_useDelay.IsDelayed(entity.Owner) || !args.ClickLocation.IsValid(EntityManager))
            return;

        var focus = _transform.ToMapCoordinates(args.ClickLocation);
        if (!float.IsFinite(focus.X) || !float.IsFinite(focus.Y) ||
            !_examine.InRangeUnOccluded(args.User, focus, entity.Comp.MaxCaptureDistance, ignoreInsideBlocker: false))
        {
            _popup.PopupEntity(Loc.GetString("photo-camera-component-invalid-focus"), entity, args.User);
            return;
        }

        TryComp<LimitedChargesComponent>(entity, out var charges);
        if (!_charges.HasCharges((entity, charges), 1))
        {
            _popup.PopupEntity(Loc.GetString("photo-camera-component-no-charges-message"), entity, args.User, PopupType.Medium);
            return;
        }

        if (!TryPhoto(entity, focus, out var photo))
            return;

        if (TryComp<HandsComponent>(args.User, out var hands))
        {
            if (_hands.TryGetEmptyHand((args.User, hands), out var emptyHand))
                _hands.TryPickup(args.User, photo.Value, emptyHand, checkActionBlocker: false, handsComp: hands);
        }

        _charges.AddCharges((entity, charges, null), -1);
        _useDelay.TryResetDelay(entity.Owner);
        _audio.PlayPvs(entity.Comp.ShotSound, entity);
        Dirty(entity);
    }

    private string GetPhotoSizeLoc(float size)
    {
        return Loc.GetString("photo-camera-component-photo-size", ("size", size));
    }

    private bool TryPhoto(Entity<PhotoCameraComponent> entity, MapCoordinates focus, [NotNullWhen(true)] out EntityUid? photoEntity)
    {
        photoEntity = null;

        if (entity.Comp.FilmLeft <= 0)
            return false;

        var xform = Transform(entity);

        Angle cameraRotation;

        if (xform.GridUid is { } gridToAllignWith)
            cameraRotation = _transform.GetWorldRotation(gridToAllignWith);
        else
            cameraRotation = _transform.GetWorldRotation(xform);

        if (!_photo.TryCapture(focus, cameraRotation, entity.Comp.SelectedPhotoDimensions, out var id, out var seenObjects))
            return false;

        photoEntity = Spawn(entity.Comp.PhotoPrototypeId, xform.MapPosition);
        var photoComp = EnsureComp<PhotoComponent>(photoEntity.Value);
        photoComp.PhotoID = id;
        photoComp.SeenObjects = seenObjects;
        Dirty(photoEntity.Value, photoComp);

        return true;
    }
}
