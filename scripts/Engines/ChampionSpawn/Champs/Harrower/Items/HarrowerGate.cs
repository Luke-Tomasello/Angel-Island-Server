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

/* Scripts\Engines\ChampionSpawn\Champs\Harrower\Items\HarrowerGate.cs
 * CHANGELOG
 *  01/05/07, Plasma
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Items
{
    public class HarrowerGate : Moongate
    {
        private Mobile m_Harrower;

        public override int LabelNumber { get { return 1049498; } } // dark moongate

        public HarrowerGate(Mobile harrower, Point3D loc, Map map, Point3D targLoc, Map targMap)
            : base(targLoc, targMap)
        {
            m_Harrower = harrower;

            Dispellable = false;
            ItemID = 0x1FD4;
            Light = LightType.Circle300;

            MoveToWorld(loc, map);
        }

        public HarrowerGate(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Harrower);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Harrower = reader.ReadMobile();

                        if (m_Harrower == null)
                            Delete();

                        break;
                    }
            }

            if (Light != LightType.Circle300)
                Light = LightType.Circle300;
        }
    }
}