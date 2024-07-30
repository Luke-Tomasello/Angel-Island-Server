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

/* Scripts\Engines\Township\Items\Fortifications\Rocks.cs
 * CHANGELOG:
 * 3/24/22, Adam
 *  Moved from Fortifications to Gardening
 * 3/20/22, Adam
 *	    Initial creation
 */

/*
 Igneous Rocks:
   Basalt, Diabase, Diorite, Gabbro, Granite, Obsidian, Pumice, Rhyolite, Scoria
Sedimentary Rocks:
   Breccia, Conglomerate, Limestone, Sandstone, Shale

Metamorphic Rocks:
   Gneiss, Marble, Quartzite, Schist, Serpentinite, Slate
 */
// rock ids
// 0x1363,  Basalt #
// 0x1364,  Dacite #
// 0x136B,  Diabase #
// 0x1772,  Diorite #
// 0x1773,  Gabbro #
// 0x1775,  Pegmatite #
// 0x1777,  Peridotite #
// 0x1367,  Rhyolite #
// 0x1774,  Gneiss
// 0x177c,  Quartzite

namespace Server.Township
{
    public class Quartzite : TownshipRock
    {
        [Constructable]
        public Quartzite()
            : this(0x177C)
        {
        }

        [Constructable]
        public Quartzite(int itemID)
            : base(itemID)
        {
            Weight = 20;
        }

        public Quartzite(Serial serial)
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
    public class Gneiss : TownshipRock
    {
        [Constructable]
        public Gneiss()
            : this(0x1774)
        {
        }

        [Constructable]
        public Gneiss(int itemID)
            : base(itemID)
        {
            Weight = 20;
        }

        public Gneiss(Serial serial)
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
    public class Rhyolite : TownshipRock
    {
        [Constructable]
        public Rhyolite()
            : this(0x1367)
        {
        }

        [Constructable]
        public Rhyolite(int itemID)
            : base(itemID)
        {
            Weight = 20;
        }

        public Rhyolite(Serial serial)
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
    public class Peridotite : TownshipRock
    {
        [Constructable]
        public Peridotite()
            : this(0x1777)
        {
        }

        [Constructable]
        public Peridotite(int itemID)
            : base(itemID)
        {
            Weight = 20;
        }

        public Peridotite(Serial serial)
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
    public class Pegmatite : TownshipRock
    {
        [Constructable]
        public Pegmatite()
            : this(0x1775)
        {
        }

        [Constructable]
        public Pegmatite(int itemID)
            : base(itemID)
        {
            Weight = 20;
        }

        public Pegmatite(Serial serial)
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
    public class Gabbro : TownshipRock
    {
        [Constructable]
        public Gabbro()
            : this(0x1773)
        {
        }

        [Constructable]
        public Gabbro(int itemID)
            : base(itemID)
        {
            Weight = 20;
        }

        public Gabbro(Serial serial)
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
    public class Diorite : TownshipRock
    {
        [Constructable]
        public Diorite()
            : this(0x1772)
        {
        }

        [Constructable]
        public Diorite(int itemID)
            : base(itemID)
        {
            Weight = 20;
        }

        public Diorite(Serial serial)
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
    public class Diabase : TownshipRock
    {
        [Constructable]
        public Diabase()
            : this(0x136B)
        {
        }

        [Constructable]
        public Diabase(int itemID)
            : base(itemID)
        {
            Weight = 20;
        }

        public Diabase(Serial serial)
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
    public class Dacite : TownshipRock
    {
        [Constructable]
        public Dacite()
            : this(0x1364)
        {
        }

        [Constructable]
        public Dacite(int itemID)
            : base(itemID)
        {
            Weight = 20;
        }

        public Dacite(Serial serial)
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
    public class Basalt : TownshipRock
    {
        [Constructable]
        public Basalt()
            : this(0x1363)
        {
        }

        [Constructable]
        public Basalt(int itemID)
            : base(itemID)
        {
            Weight = 20;
        }

        public Basalt(Serial serial)
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

    public class TownshipRock : TownshipStatic
    {
        public TownshipRock(int itemID)
            : base(itemID)
        {
            Weight = 20;
        }

        public TownshipRock(Serial serial)
            : base(serial)
        {
        }

        public override void OnBuild(Mobile from)
        {
            base.OnBuild(from);

            int hits = 100;

            this.HitsMax = hits;
            this.Hits = hits;
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