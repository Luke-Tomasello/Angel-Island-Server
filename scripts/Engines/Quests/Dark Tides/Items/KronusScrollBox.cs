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
using Server.Mobiles;

namespace Server.Engines.Quests.Necro
{
    public class KronusScrollBox : MetalBox
    {
        [Constructable]
        public KronusScrollBox()
        {
            ItemID = 0xE80;
            Movable = false;

            for (int i = 0; i < 40; i++)
            {
                Item scroll = Loot.RandomScroll(0, 15, SpellbookType.Necromancer);
                scroll.Movable = false;
                DropItem(scroll);
            }
        }

        public KronusScrollBox(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            PlayerMobile pm = from as PlayerMobile;

            if (pm != null && pm.InRange(GetWorldLocation(), 2))
            {
                QuestSystem qs = pm.Quest;

                if (qs is DarkTidesQuest)
                {
                    QuestObjective obj = qs.FindObjective(typeof(FindCallingScrollObjective));

                    if ((obj != null && !obj.Completed) || DarkTidesQuest.HasLostCallingScroll(from))
                    {
                        Item scroll = new KronusScroll();

                        if (pm.PlaceInBackpack(scroll))
                        {
                            pm.SendLocalizedMessage(1060120, "", 0x41); // You rummage through the scrolls until you find the Scroll of Calling.  You quickly put it in your pack.

                            if (obj != null && !obj.Completed)
                                obj.Complete();
                        }
                        else
                        {
                            pm.SendLocalizedMessage(1060148, "", 0x41); // You were unable to take the scroll.
                            scroll.Delete();
                        }
                    }
                }
            }

            base.OnDoubleClick(from);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}