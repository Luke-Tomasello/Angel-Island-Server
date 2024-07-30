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

/* Scripts\Engines\Township\Craft\TownshipCommodityDeeds.cs
 * CHANGELOG:
 * 3/20/22, Adam
 *  Initial version.
 */

using Server.Items;
using System;

namespace Server.Township
{
    public class QuartziteDeed : BaseTSDeed
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public override Type InvoiceType { get { return typeof(Quartzite); } }

        [Constructable]
        public QuartziteDeed()
            : this(10) { }

        [Constructable]
        public QuartziteDeed(int quantity)
            : base(quantity) { }

        public QuartziteDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    public class GneissDeed : BaseTSDeed
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public override Type InvoiceType { get { return typeof(Gneiss); } }

        [Constructable]
        public GneissDeed()
            : this(10) { }

        [Constructable]
        public GneissDeed(int quantity)
            : base(quantity) { }

        public GneissDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    public class BasaltDeed : BaseTSDeed
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public override Type InvoiceType { get { return typeof(Basalt); } }

        [Constructable]
        public BasaltDeed()
            : this(10) { }

        [Constructable]
        public BasaltDeed(int quantity)
            : base(quantity) { }

        public BasaltDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    public class DaciteDeed : BaseTSDeed
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public override Type InvoiceType { get { return typeof(Dacite); } }

        [Constructable]
        public DaciteDeed()
            : this(10) { }

        [Constructable]
        public DaciteDeed(int quantity)
            : base(quantity) { }

        public DaciteDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    public class DiabaseDeed : BaseTSDeed
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public override Type InvoiceType { get { return typeof(Diabase); } }

        [Constructable]
        public DiabaseDeed()
            : this(10) { }

        [Constructable]
        public DiabaseDeed(int quantity)
            : base(quantity) { }

        public DiabaseDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    public class DioriteDeed : BaseTSDeed
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public override Type InvoiceType { get { return typeof(Diorite); } }

        [Constructable]
        public DioriteDeed()
            : this(10) { }

        [Constructable]
        public DioriteDeed(int quantity)
            : base(quantity) { }

        public DioriteDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    public class GabbroDeed : BaseTSDeed
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public override Type InvoiceType { get { return typeof(Gabbro); } }

        [Constructable]
        public GabbroDeed()
            : this(10) { }

        [Constructable]
        public GabbroDeed(int quantity)
            : base(quantity) { }

        public GabbroDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    public class PegmatiteDeed : BaseTSDeed
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public override Type InvoiceType { get { return typeof(Pegmatite); } }

        [Constructable]
        public PegmatiteDeed()
            : this(10) { }

        [Constructable]
        public PegmatiteDeed(int quantity)
            : base(quantity) { }

        public PegmatiteDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    public class PeridotiteDeed : BaseTSDeed
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public override Type InvoiceType { get { return typeof(Peridotite); } }

        [Constructable]
        public PeridotiteDeed()
            : this(10) { }

        [Constructable]
        public PeridotiteDeed(int quantity)
            : base(quantity) { }

        public PeridotiteDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    public class RhyoliteDeed : BaseTSDeed
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public override Type InvoiceType { get { return typeof(Rhyolite); } }

        [Constructable]
        public RhyoliteDeed()
            : this(10) { }

        [Constructable]
        public RhyoliteDeed(int quantity)
            : base(quantity) { }

        public RhyoliteDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    public abstract class BaseTSDeed : Item, IHasQuantity
    {
        private int m_Quantity;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Quantity
        {
            get { return m_Quantity; }
            set { m_Quantity = value; InvalidateProperties(); }
        }

        public BaseTSDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Quantity);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Quantity = reader.ReadInt();
                        break;
                    }
            }
        }

        public BaseTSDeed(int quantity)
            : base(0x14F0)
        {
            Weight = 1.0;
            m_Quantity = quantity;
            Name = DefaultName;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060738, m_Quantity.ToString()); // value: ~1_val~
        }
        public override string DefaultName => InvoiceType.Name;
        public abstract Type InvoiceType { get; }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, string.Format("an invoice for {0} {1}", m_Quantity, InvoiceType.Name));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Deleted)
                return;
        }
    }
}