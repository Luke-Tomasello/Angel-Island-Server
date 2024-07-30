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

/* Scripts/Items/Misc/ResGate.cs
 * ChangeLog
 *  11/6/06, Rhiannon
 *		Fixed typo in name.
 *	1/21/05, Adam
 *		Invoke the non-statloss version of the ResurrectGump.
 */

using Server.Gumps;
using Server.Mobiles;

namespace Server.Items
{
    public class ResGate : Item
    {
        [Constructable]
        public ResGate()
            : base(0xF6C)
        {
            Movable = false;
            Hue = 0x2D1;
            Name = "a resurrection gate";
            Light = LightType.Circle300;
        }

        public ResGate(Serial serial)
            : base(serial)
        {
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (!m.Alive && m.Map != null && Utility.CanFit(m.Map, m.Location, 16, Utility.CanFitFlags.requireSurface))
            {
                m.PlaySound(0x214);
                m.FixedEffect(0x376A, 10, 16);

                // Adam: when using this gate, we turn off statloss
                if (m != null && m is PlayerMobile)
                {
                    PlayerMobile pm = m as PlayerMobile;
                    m.SendGump(new ResurrectGump(pm, false));
                }
            }
            else
            {
                m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
            }

            return false;
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