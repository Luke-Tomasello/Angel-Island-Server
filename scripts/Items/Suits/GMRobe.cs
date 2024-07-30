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

/* Scripts/Items/Suits/GMRobe.cs
 * ChangeLog
 *	6/27/06, Adam
 *		- (re)set the player mobile titles automatically
 *		- clear fame and karma (titles)
 *		- reset color on robe load
 *	4/17/06, Adam
 *		Explicitly set name
 *	11/07/04, Jade
 *		Changed hue to 0x485.
 */

namespace Server.Items
{
    public class GMRobe : BaseSuit
    {
        private const int m_hue = 0x485;    // Game master color

        [Constructable]
        public GMRobe()
            : base(AccessLevel.GameMaster, m_hue, 0x204F)
        {
            Name = "Gamemaster Robe";
        }

        public GMRobe(Serial serial)
            : base(serial)
        {
        }

        public override bool OnEquip(Mobile from)
        {
            if (base.OnEquip(from) == true)
            {
                from.Title = null;
                from.Fame = 0;
                from.Karma = 0;
                return true;
            }
            else return false;
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

            if (Hue != m_hue)
                Hue = m_hue;
        }
    }
}