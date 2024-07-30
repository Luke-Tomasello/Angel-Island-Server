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

/* Items/Facial/Hair.cs
 * ChangeLog:
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Items
{
    public abstract class Hair : Item
    {
        public static Hair CreateByID(int id, int hue)
        {
            switch (id)
            {
                case 0x203B: return new ShortHair(hue);
                case 0x203C: return new LongHair(hue);
                case 0x203D: return new PonyTail(hue);
                case 0x2044: return new Mohawk(hue);
                case 0x2045: return new PageboyHair(hue);
                case 0x2046: return new BunsHair(hue);
                case 0x2047: return new Afro(hue);
                case 0x2048: return new ReceedingHair(hue);
                case 0x2049: return new TwoPigTails(hue);
                case 0x204A: return new KrisnaHair(hue);
                default: return new GenericHair(id, hue);
            }
        }

        public Hair(int itemID)
            : this(itemID, 0)
        {
        }

        public Hair(int itemID, int hue)
            : base(itemID)
        {
            LootType = LootType.Blessed;
            Layer = Layer.Hair;
            Hue = hue;
        }

        public Hair(Serial serial)
            : base(serial)
        {
        }

        public override bool DisplayLootType { get { return false; } }

        public override bool VerifyMove(Mobile from)
        {
            return (from.AccessLevel >= AccessLevel.GameMaster);
        }

        public override DeathMoveResult OnParentDeath(Mobile parent)
        {
            Dupe(Amount);

            return DeathMoveResult.MoveToCorpse;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            LootType = LootType.Blessed;

            int version = reader.ReadInt();
        }
    }

    public class GenericHair : Hair
    {
        [Constructable]
        public GenericHair(int itemID)
            : this(itemID, 0)
        {
        }

        [Constructable]
        public GenericHair(int itemID, int hue)
            : base(itemID, hue)
        {
        }

        public GenericHair(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new GenericHair(ItemID, Hue), amount);
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

    public class Mohawk : Hair
    {
        [Constructable]
        public Mohawk()
            : this(0)
        {
        }

        [Constructable]
        public Mohawk(int hue)
            : base(0x2044, hue)
        {
        }

        public Mohawk(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Mohawk(), amount);
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

    public class PageboyHair : Hair
    {
        [Constructable]
        public PageboyHair()
            : this(0)
        {
        }

        [Constructable]
        public PageboyHair(int hue)
            : base(0x2045, hue)
        {
        }

        public PageboyHair(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new PageboyHair(), amount);
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

    public class BunsHair : Hair
    {
        [Constructable]
        public BunsHair()
            : this(0)
        {
        }

        [Constructable]
        public BunsHair(int hue)
            : base(0x2046, hue)
        {
        }

        public BunsHair(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new BunsHair(), amount);
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

    public class LongHair : Hair
    {
        [Constructable]
        public LongHair()
            : this(0)
        {
        }

        [Constructable]
        public LongHair(int hue)
            : base(0x203C, hue)
        {
        }

        public LongHair(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new LongHair(), amount);
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

    public class ShortHair : Hair
    {
        [Constructable]
        public ShortHair()
            : this(0)
        {
        }

        [Constructable]
        public ShortHair(int hue)
            : base(0x203B, hue)
        {
        }

        public ShortHair(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new ShortHair(), amount);
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

    public class PonyTail : Hair
    {
        [Constructable]
        public PonyTail()
            : this(0)
        {
        }

        [Constructable]
        public PonyTail(int hue)
            : base(0x203D, hue)
        {
        }

        public PonyTail(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new PonyTail(), amount);
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

    public class Afro : Hair
    {
        [Constructable]
        public Afro()
            : this(0)
        {
        }

        [Constructable]
        public Afro(int hue)
            : base(0x2047, hue)
        {
        }

        public Afro(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Afro(), amount);
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

    public class ReceedingHair : Hair
    {
        [Constructable]
        public ReceedingHair()
            : this(0)
        {
        }

        [Constructable]
        public ReceedingHair(int hue)
            : base(0x2048, hue)
        {
        }

        public ReceedingHair(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new ReceedingHair(), amount);
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

    public class TwoPigTails : Hair
    {
        [Constructable]
        public TwoPigTails()
            : this(0)
        {
        }

        [Constructable]
        public TwoPigTails(int hue)
            : base(0x2049, hue)
        {
        }

        public TwoPigTails(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new TwoPigTails(), amount);
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

    public class KrisnaHair : Hair
    {
        [Constructable]
        public KrisnaHair()
            : this(0)
        {
        }

        [Constructable]
        public KrisnaHair(int hue)
            : base(0x204A, hue)
        {
        }

        public KrisnaHair(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new KrisnaHair(), amount);
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