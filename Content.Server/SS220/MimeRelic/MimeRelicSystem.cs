using Content.Server.Popups;

namespace Content.Server.SS220.MimeRelic

public sealed class MimeRelicSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public void override Initialize()
    {
        SubscribeLocalEvent<MimeRelicComponent, ActivateInWorldEvent>(OnMimeRelicActivate);
    }


    private OnMimeRelicActivate(Entity<MimeRelicComponent> ent, MimeRelicComponent component, ActivateInWorldEvent args)
    {
        if (HasComp<MindContainerComponent>(ent.uid) == false)
        {
            // sendmsgToPopup 'text-not-mime'
            return;
        }

        if (_container.IsEntityOrParentInContainer(uid))
        {    
            // sendmsgToPopup 'text-failed-place'
            return;        
        }

        if (OnCooldown())
        {
           // sendmsgToPopup 'text-on-cooldown'
           return;
        }

        if (CanPlaceWall(ent))
        {
            // sendmsgToPopup 'text-successed-place'
            //place additional walls near first
            
        }

        // sendmsgToPopup 'text-failed-place'
    }

    private CanPlaceWall(Entity ent)
    {
        //check on correct place with tile
        //check if there is wall infront of user

    }
}