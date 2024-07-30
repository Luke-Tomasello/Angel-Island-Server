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

/* Items/Misc/Stonebag.cs
 * ChangeLog:
 * 4/2/10, adam
 *     Initial Version
 */

namespace Server.Items
{
    /// <summary>
    /// Summary description for Stonebag.
    /// </summary>
    public class Stonebag : Bag
    {
        [Constructable]
        public Stonebag()
            : base()
        {
            Name = "stonebag";
            Hue = 0x33;
            MaxItems = Utility.RandomMinMax(9, 30);
            LootType = LootType.Newbied;
        }

        public Stonebag(Serial serial)
            : base(serial)
        {
        }

        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            if (item is Moonstone == false)
            {
                if (message)
                    m.SendMessage("You may only store moonstones in this bag.");

                return false;
            }

            return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            bool bReturn = false;
            if (item is Moonstone)
            {
                bReturn = base.OnDragDrop(from, item);
                if (bReturn)
                {
                    // if ok,,,
                }
            }
            else
            {
                from.SendMessage("You may only store moonstones in there.");
                return false;
            }

            return bReturn;
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