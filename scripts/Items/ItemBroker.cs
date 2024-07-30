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

/* Scripts/Items/ItemBroker.cs
 * CHANGELOG:
 *	01/03/07 - Pix
 *		Finally got back to this - tested and good!
 *	11/28/06 - Pix
 *		Initial Version.
 */

using System.Collections.Generic;

namespace Server.Items
{
    public class ItemBroker
    {
        public static T GetItem<T>(Serial serial, ref bool isFD) where T : Item
        {
            return GetItem(serial, ref isFD) as T;
        }

        private static Item GetItem(Serial serial, ref bool isFD)
        {
            isFD = false; //initialize

            Item item = World.FindItem(serial);
            if (item != null)
            {
                return item;
            }
            //else
            //{
            //    if (World.IsReserved(serial))
            //    {
            //        isFD = true;
            //    }
            //}

            return null;
        }

        public static void WriteSerialList(GenericWriter writer, List<Serial> serialList)
        {
            writer.Write(serialList.Count);
            for (int i = 0; i < serialList.Count; i++)
            {
                writer.Write((int)serialList[i]);
            }
        }

        public static List<Serial> ReadSerialList(GenericReader reader)
        {
            int count = reader.ReadInt();

            List<Serial> list = new List<Serial>(count);
            for (int i = 0; i < count; i++)
            {
                int s = reader.ReadInt();
                list.Add((Serial)s);
            }

            return list;
        }
    }
}