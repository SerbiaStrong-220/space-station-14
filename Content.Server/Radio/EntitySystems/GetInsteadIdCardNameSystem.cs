using Content.Server.Radio.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Silicons.Borgs.Components;
using System.Globalization;

public sealed class GetInsteadIdCardName : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BorgChassisComponent, GetInsteadIdCardNameEvent>(OnGetBorgName);
        SubscribeLocalEvent<InventoryComponent, GetInsteadIdCardNameEvent>(OnGetIdCardName);
    }

    private void OnGetBorgName(EntityUid uid, BorgChassisComponent component, ref GetInsteadIdCardNameEvent args)
    {
        if (HasComp<BorgChassisComponent>(uid) && TryPrototype(uid, out var borgPototype))
        {
            string borJob = borgPototype.Name.ToString();
            args.Name = $"\\[{borJob}\\] ";
        }
        else
            args.Name = string.Empty;
    }

    private void OnGetIdCardName(EntityUid uid, InventoryComponent invent, ref GetInsteadIdCardNameEvent args)
    {
        var idCard = new IdCardComponent();
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        string idCardName = Loc.GetString("chat-radio-no-id");
        idCardName = textInfo.ToTitleCase(idCardName);
        // Проверка слота
        if (!_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
        {
            args.Name = $"\\[{idCardName}\\] ";
            return;
        }
        // проверка пда в слоте
        if (EntityManager.TryGetComponent(idUid, out PdaComponent? pda)
            && pda.ContainedId is not null)
        {
            // проверка карты в пда
            if (TryComp<IdCardComponent>(pda.ContainedId, out idCard)) { }
        }
        // проверка карты в слоте
        else if (!TryComp<IdCardComponent>(idUid, out idCard))
            return;
        //формируем ответ
        if (idCard != null)
        {
            idCardName = idCard?.LocalizedJobTitle ?? idCardName;
            idCardName = textInfo.ToTitleCase(idCardName);
            args.Name = $"\\[{idCardName}\\] ";
        }
        // если карту не нашли, отправляем "[Без ID]"
        else
            args.Name = $"\\[{idCardName}\\] ";
    }
}
