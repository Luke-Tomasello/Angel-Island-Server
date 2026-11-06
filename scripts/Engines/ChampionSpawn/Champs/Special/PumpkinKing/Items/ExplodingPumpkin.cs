/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Engines\ChampionSpawn\Champs\Special\PumpkinKing\Items\ExplodingPumpkin.cs
 * CHANGELOG
 *  10/28/2026, Adam
 *      Initial version: thrown by The Pumpkin King. A greater explosion potion that looks like a pumpkin, explodes
 *      a few seconds after landing (no countdown), and keeps ticking if picked up. Cannot be armed or thrown by players.
 */

using System;

namespace Server.Items
{
    public class ExplodingPumpkin : GreaterExplosionPotion
    {
        public const int PumpkinID = 0xC6A;
        public const int PumpkinHue = 0;

        private Mobile? m_Thrower;

        public ExplodingPumpkin(Mobile thrower)
            : base()
        {
            m_Thrower = thrower;
            ItemID = PumpkinID;
            Hue = PumpkinHue;
            Name = "an exploding pumpkin";
            Stackable = false;
            Movable = true;
        }

        // place on the ground and light the fuse
        public void Land(Point3D loc, Map map)
        {
            MoveToWorld(loc, map);
            Timer.DelayCall(TimeSpan.FromSeconds(1.5 + Utility.RandomDouble()), Detonate);
        }

        private void Detonate()
        {
            if (Deleted || m_Thrower == null)
                return;

            object parent = FindParent(m_Thrower);

            Point3D loc;
            Map map;

            if (parent is Item item)
            {
                loc = item.GetWorldLocation();
                map = item.Map;
            }
            else if (parent is Mobile m)
            {
                loc = m.Location;
                map = m.Map;
            }
            else
            {
                Delete();
                return;
            }

            Explode(m_Thrower, true, loc, map);
        }

        // players may carry it, but not arm or throw it
        public override void OnDoubleClick(Mobile from)
        {
        }

        public ExplodingPumpkin(Serial serial)
            : base(serial)
        {
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

            // the fuse does not survive a restart
            Timer.DelayCall(TimeSpan.Zero, Delete);
        }
    }
}
