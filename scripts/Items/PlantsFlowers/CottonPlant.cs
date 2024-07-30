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

/* Scripts/Items/FlowersPlants/CottonPlant.cs
 *	ChangeLog:
 *	8/20/23, Yoar
 *	    Merged farmable crops from RunUO.
 *	    From now on, use FarmableCotton instead of CottonPlant
 *  2/11/07, Pix
 *		Finally added range check to picking cotton.
 *	5/26/04 Created by smerX
 */

using System;

namespace Server.Items
{
    [Obsolete("Use FarmableCotton instead.")]
    public class CottonPlant : Item
    {
        [Constructable]
        public CottonPlant()
            : base(Utility.RandomList(0xc51, 0xc52, 0xc53, 0xc54))
        {
            Weight = 0;
            Name = "a cotton plant";
            Movable = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 1))
            {
                SendLocalizedMessageTo(from, 501816); // You are too far away to do that.
            }
            else
            {
                Cotton cotton = new Cotton();
                cotton.MoveToWorld(new Point3D(this.X, this.Y, this.Z), this.Map);

                this.Delete();
            }
        }

        public CottonPlant(Serial serial)
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