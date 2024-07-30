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

/* Scripts/Items/FlowersPlants/HealingHerb.cs
 *	ChangeLog:
 *	3/21/2024, Adam
 *	    Updated to be derived from StackableItem
 *	3/18/2024, Adam
 *	    Initial checkin
 */

namespace Server.Items
{
    public class HealingHerb : StackableItem
    {
        private static int[] south = new int[] { 0x18E3, 0x18E1, 0x18E4, 0x18E2 };
        private static int[] east = new int[] { 0x18E4, 0x18E2, 0x18E3, 0x18E1 };
        [Constructable]
        public HealingHerb()
            : base("a healing herb", Utility.RandomBool() ? east : south)
        {

        }

        public HealingHerb(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}