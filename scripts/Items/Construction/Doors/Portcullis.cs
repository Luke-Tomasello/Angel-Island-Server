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

/* Scripts/Construction/Doors/Portcullis.cs
 * CHANGELOG
 * 
 *	9/01/06 Taran Kain
 *		Modified constructors to fit new BaseDoor constructor
 */

namespace Server.Items
{
    public class PortcullisNS : BaseDoor
    {
        public override bool UseChainedFunctionality { get { return true; } }

        [Constructable]
        public PortcullisNS()
            : base(0, 0, 0x6F5, 0x6F5, 0xF0, 0xEF, DoorFacing.EastCCW, new Point3D(0, 0, 20))
        {
        }

        public PortcullisNS(Serial serial)
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

    public class PortcullisEW : BaseDoor
    {
        public override bool UseChainedFunctionality { get { return true; } }

        [Constructable]
        public PortcullisEW()
            : base(0, 0, 0x6F6, 0x6F6, 0xF0, 0xEF, DoorFacing.EastCCW, new Point3D(0, 0, 20))
        {
        }

        public PortcullisEW(Serial serial)
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