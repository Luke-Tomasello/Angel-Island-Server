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

namespace Server.Multis
{
    public class MediumBoat : BaseBoat
    {
        public override int NorthID { get { return 0x4008; } }
        public override int EastID { get { return 0x4009; } }
        public override int SouthID { get { return 0x400A; } }
        public override int WestID { get { return 0x400B; } }

        public override int HoldDistance { get { return 4; } }
        public override int TillerManDistance { get { return -5; } }

        public override Point2D StarboardOffset { get { return new Point2D(2, 0); } }
        public override Point2D PortOffset { get { return new Point2D(-2, 0); } }

        public override Point3D MarkOffset { get { return new Point3D(0, 1, 3); } }

        public override BaseDockedBoat DockedBoat { get { return new MediumDockedBoat(this); } }

        [Constructable]
        public MediumBoat()
        {
        }

        public MediumBoat(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
    }

    public class MediumBoatDeed : BaseBoatDeed
    {
        public override int LabelNumber { get { return 1041207; } } // medium ship deed
        public override BaseBoat Boat { get { return new MediumBoat(); } }

        [Constructable]
        public MediumBoatDeed()
            : base(0x4008, Point3D.Zero)
        {
        }

        public MediumBoatDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
    }

    public class MediumDockedBoat : BaseDockedBoat
    {
        public override BaseBoat Boat { get { return new MediumBoat(); } }

        public MediumDockedBoat(BaseBoat boat)
            : base(0x4008, Point3D.Zero, boat)
        {
        }

        public MediumDockedBoat(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
    }
}