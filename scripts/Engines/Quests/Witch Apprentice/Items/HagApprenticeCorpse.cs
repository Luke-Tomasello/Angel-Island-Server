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
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using System.Collections;

namespace Server.Engines.Quests.Hag
{
    public class HagApprenticeCorpse : Corpse
    {
        private static Mobile GetOwner()
        {
            Mobile apprentice = new Mobile();

            apprentice.Hue = Utility.RandomSkinHue();
            apprentice.Female = false;
            apprentice.Body = 0x190;

            apprentice.Delete();

            return apprentice;
        }

        private static ArrayList GetEquipment()
        {
            return new ArrayList();
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add("a charred corpse");
        }

        public override void OnSingleClick(Mobile from)
        {
            int hue = Notoriety.GetHue(NotorietyHandlers.CorpseNotoriety(from, this));

            from.Send(new AsciiMessage(Serial, ItemID, MessageType.Label, hue, 3, "", "a charred corpse"));
        }

        [Constructable]
        public HagApprenticeCorpse()
            : base(GetOwner(), GetEquipment())
        {
            Direction = Direction.South;

            foreach (Item item in EquipItems)
            {
                DropItem(item);
            }
        }

        public HagApprenticeCorpse(Serial serial)
            : base(serial)
        {
        }

        public override void Open(Mobile from, bool checkSelfLoot)
        {
            if (!from.InRange(this.GetWorldLocation(), 2))
                return;

            PlayerMobile player = from as PlayerMobile;

            if (player != null)
            {
                QuestSystem qs = player.Quest;

                if (qs is WitchApprenticeQuest)
                {
                    FindApprenticeObjective obj = qs.FindObjective(typeof(FindApprenticeObjective)) as FindApprenticeObjective;

                    if (obj != null && !obj.Completed)
                    {
                        if (obj.Corpse == this)
                        {
                            obj.Complete();
                            Delete();
                        }
                        else
                        {
                            SendLocalizedMessageTo(from, 1055047); // You examine the corpse, but it doesn't fit the description of the particular apprentice the Hag tasked you with finding.
                        }

                        return;
                    }
                }
            }

            SendLocalizedMessageTo(from, 1055048); // You examine the corpse, but find nothing of interest.
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