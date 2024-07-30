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

/* Scripts\Engines\Ethics\Evil\Powers\UnholyBless.cs
 * ChangeLog
 *  1/10/23, Yoar
 *      Initial commit
 */

using Server.Items;

namespace Server.Ethics.Evil
{
    public sealed class UnholyBless : Power
    {
        public UnholyBless()
        {
            m_Definition = new PowerDefinition(
                    60,
                    0,
                    "Bless",
                    "Velgo Reyam",
                    "This will bless an item for 30 minutes."
                );
        }

        public override void BeginInvoke(Player from)
        {
            from.Mobile.BeginTarget(12, false, Targeting.TargetFlags.None, new TargetStateCallback(Power_OnTarget), from);
            from.Mobile.SendLocalizedMessage(1045106); // Target the item you wish to bless.
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

            if (item.CheckBlessed(item.Parent as Mobile) || EthicBless.GetBlessedFor(item) != null)
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, 1045113); // That item is already blessed
                return;
            }

            bool canImbue = (item is Spellbook || item is BaseClothing || item is BaseArmor || item is BaseWeapon) && (item.Name == null);

            if (item.LootType != LootType.Regular || !canImbue)
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, 1045114); // You cannot bless that item
                return;
            }

            if (CheckInvoke(from))
            {
                EthicBless.EvilBless(item);

                from.Mobile.FixedEffect(0x375A, 10, 20);
                from.Mobile.PlaySound(0x209);

                FinishInvoke(from);
            }
        }
    }
}