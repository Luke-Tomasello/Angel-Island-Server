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

/* ShipHullCraft.cs
 * CHANGELOG:
 *  5/15/2026, Adam
 */

namespace Server.Items
{
    public class SmallBoatHullCraft : BaseShipPartCraft
    {
        protected override BoatType? RequiredBoatType { get { return BoatType.SmallBoat; } }
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Hull; } }

        [Constructable]
        public SmallBoatHullCraft()
            : base(1)
        {
            Name = "small boat hull";
        }

        public SmallBoatHullCraft(Serial serial)
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

    public class SmallDragonBoatHullCraft : BaseShipPartCraft
    {
        protected override BoatType? RequiredBoatType { get { return BoatType.SmallDragonBoat; } }
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Hull; } }

        [Constructable]
        public SmallDragonBoatHullCraft()
            : base(1)
        {
            Name = "small dragon boat hull";
        }

        public SmallDragonBoatHullCraft(Serial serial)
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

    public class MediumBoatHullCraft : BaseShipPartCraft
    {
        protected override BoatType? RequiredBoatType { get { return BoatType.MediumBoat; } }
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Hull; } }

        [Constructable]
        public MediumBoatHullCraft()
            : base(1)
        {
            Name = "medium boat hull";
        }

        public MediumBoatHullCraft(Serial serial)
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

    public class MediumDragonBoatHullCraft : BaseShipPartCraft
    {
        protected override BoatType? RequiredBoatType { get { return BoatType.MediumDragonBoat; } }
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Hull; } }

        [Constructable]
        public MediumDragonBoatHullCraft()
            : base(1)
        {
            Name = "medium dragon boat hull";
        }

        public MediumDragonBoatHullCraft(Serial serial)
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

    public class LargeBoatHullCraft : BaseShipPartCraft
    {
        protected override BoatType? RequiredBoatType { get { return BoatType.LargeBoat; } }
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Hull; } }

        [Constructable]
        public LargeBoatHullCraft()
            : base(1)
        {
            Name = "large boat hull";
        }

        public LargeBoatHullCraft(Serial serial)
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

    public class LargeDragonBoatHullCraft : BaseShipPartCraft
    {
        protected override BoatType? RequiredBoatType { get { return BoatType.LargeDragonBoat; } }
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Hull; } }

        [Constructable]
        public LargeDragonBoatHullCraft()
            : base(1)
        {
            Name = "large dragon boat hull";
        }

        public LargeDragonBoatHullCraft(Serial serial)
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