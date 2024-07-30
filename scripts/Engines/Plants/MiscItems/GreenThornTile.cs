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

/* Scripts\Engines\Plants\MiscItems\GreenThornTile.cs
 *	ChangeLog :
 *	3/20/2024, Adam
 *		Created. 
 */

namespace Server.Items
{
    public class GreenThornTile : Item
    {
        private static Map m_Map = Map.Ilshenar;
        private static Point3D m_PointDestination = new Point3D(1534, 1538, -2);
        [CommandProperty(AccessLevel.GameMaster)]
        public static Map MapDestination { get { return m_Map; } set { m_Map = value; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public static Point3D PointDestination { get { return m_PointDestination; } set { m_PointDestination = value; } }

        [Constructable]
        public GreenThornTile()
            : base(0x495)
        {
            Hue = 1;
            Movable = false;
            Name = "a hole";
        }

        public GreenThornTile(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new GreenThornTile(amount), amount);
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