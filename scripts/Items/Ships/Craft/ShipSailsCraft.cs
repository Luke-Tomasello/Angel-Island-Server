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

/* ShipSailsCraft.cs
 * CHANGELOG:
 *  5/15/2026, Adam
 */

namespace Server.Items
{
    public class SmallBoatSailsCraft : BaseShipPartCraft
    {
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Sails; } }
        protected override BoatType? RequiredBoatType { get { return BoatType.SmallBoat; } }

        [Constructable]
        public SmallBoatSailsCraft()
            : base(1)
        {
            Name = "small boat sails";
        }

        public SmallBoatSailsCraft(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class SmallDragonBoatSailsCraft : BaseShipPartCraft
    {
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Sails; } }
        protected override BoatType? RequiredBoatType { get { return BoatType.SmallDragonBoat; } }

        [Constructable]
        public SmallDragonBoatSailsCraft()
            : base(1)
        {
            Name = "small dragon boat sails";
        }

        public SmallDragonBoatSailsCraft(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class MediumBoatSailsCraft : BaseShipPartCraft
    {
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Sails; } }
        protected override BoatType? RequiredBoatType { get { return BoatType.MediumBoat; } }

        [Constructable]
        public MediumBoatSailsCraft()
            : base(1)
        {
            Name = "medium boat sails";
        }

        public MediumBoatSailsCraft(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class MediumDragonBoatSailsCraft : BaseShipPartCraft
    {
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Sails; } }
        protected override BoatType? RequiredBoatType { get { return BoatType.MediumDragonBoat; } }

        [Constructable]
        public MediumDragonBoatSailsCraft()
            : base(1)
        {
            Name = "medium dragon boat sails";
        }

        public MediumDragonBoatSailsCraft(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class LargeBoatSailsCraft : BaseShipPartCraft
    {
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Sails; } }
        protected override BoatType? RequiredBoatType { get { return BoatType.LargeBoat; } }

        [Constructable]
        public LargeBoatSailsCraft()
            : base(1)
        {
            Name = "large boat sails";
        }

        public LargeBoatSailsCraft(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class LargeDragonBoatSailsCraft : BaseShipPartCraft
    {
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Sails; } }
        protected override BoatType? RequiredBoatType { get { return BoatType.LargeDragonBoat; } }

        [Constructable]
        public LargeDragonBoatSailsCraft()
            : base(1)
        {
            Name = "large dragon boat sails";
        }

        public LargeDragonBoatSailsCraft(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
