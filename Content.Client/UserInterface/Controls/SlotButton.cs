﻿using static Content.Client.Inventory.ClientInventorySystem;

namespace Content.Client.UserInterface.Controls
{
    public sealed class SlotButton : SlotControl
    {
        public SlotButton() { }

        public SlotButton(SlotData slotData)
        {
            ButtonTexturePath = slotData.TextureName;
            FullButtonTexturePath = slotData.FullTextureName;
            Blocked = slotData.Blocked;
            Highlight = slotData.Highlighted;
            StuckOnEquip = slotData.StuckOnEquip; //ss220 StuckOnEquip
            StorageTexturePath = "Slots/back";
            SlotName = slotData.SlotName;
        }
    }
}
