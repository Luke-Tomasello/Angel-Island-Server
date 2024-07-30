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

using Server.Mobiles;

namespace Server.Engines.Quests
{
    public abstract class DynamicTeleporter : Item
    {
        public override int LabelNumber { get { return 1049382; } } // a magical teleporter

        public DynamicTeleporter()
            : base(0x1822)
        {
            Movable = false;
            Hue = 0x482;
        }

        public abstract bool GetDestination(PlayerMobile player, ref Point3D loc, ref Map map);

        public override bool OnMoveOver(Mobile m)
        {
            PlayerMobile pm = m as PlayerMobile;

            if (pm != null)
            {
                Point3D loc = Point3D.Zero;
                Map map = null;

                if (GetDestination(pm, ref loc, ref map))
                {
                    BaseCreature.TeleportPets(pm, loc, map);

                    pm.PlaySound(0x1FE);
                    pm.MoveToWorld(loc, map);

                    return false;
                }
                else
                {
                    pm.SendLocalizedMessage(500309); // Nothing Happens.
                }
            }

            return base.OnMoveOver(m);
        }

        public DynamicTeleporter(Serial serial)
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
        }
    }
}