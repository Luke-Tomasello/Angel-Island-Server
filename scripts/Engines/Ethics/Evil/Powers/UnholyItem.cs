/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using Server.Items;

namespace Server.Ethics.Evil
{
    public sealed class UnholyItem : Power
    {
        public UnholyItem()
        {
            m_Definition = new PowerDefinition(
                    Core.NewEthics ? 5 : 1,
                    1,
                    Core.NewEthics ? "Vile Item" : "Black Armor",
                    "Vidda K'balc",
                    "Turn any one item into the color true black."
                );
        }

        public override void BeginInvoke(Player from)
        {
            from.Mobile.BeginTarget(12, false, Targeting.TargetFlags.None, new TargetStateCallback(Power_OnTarget), from);
            from.Mobile.SendLocalizedMessage(1045104); // Target the item you wish to hue
        }

        private void Power_OnTarget(Mobile fromMobile, object obj, object state)
        {
            Player from = state as Player;

            Item item = obj as Item;

            if (item == null)
                return;

            if (item.Parent != from.Mobile)
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, 1045110); // You may only use this power on items you have equipped!
                return;
            }

            if (Ethic.Find(item) != null)
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, 1045111); // This item is already hued
                return;
            }

            // in 13.6, factions and ethics were merged and I believe this is when all the extra huing was added.
            //	the original ethics only allowed the huing of armor
            // https://www.uoguide.com/Siege_Faction
            bool canImbue;
            if (Core.OldEthics)
                canImbue = (item is BaseArmor && item.Name == null);
            else
                canImbue = (item is Spellbook || item is BaseClothing || item is BaseArmor || item is BaseWeapon) && (item.Name == null);

            if (!canImbue)
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, 1045112); // You cannot hue that item
                return;
            }

            if (CheckInvoke(from))
            {
                item.Hue = Ethic.Evil.Definition.PrimaryHue;
                item.SavedFlags |= 0x200;

                from.Mobile.FixedEffect(0x375A, 10, 20);
                from.Mobile.PlaySound(0x209);

                FinishInvoke(from);
            }
        }
    }
}