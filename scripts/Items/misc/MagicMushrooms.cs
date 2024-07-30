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

/* Scripts\Items\Misc\MagicMushrooms.cs
 * ChangeLog
 *	11/11/21, Adam
 *		initial creation
 */

using System;

namespace Server.Items
{
    public abstract class BaseMagicMushroom : Item
    {
        public virtual int Bonus { get { return 0; } }
        public virtual StatType Type { get { return StatType.Str; } }

        public BaseMagicMushroom(int itemID)
            : base(itemID)
        {
            Weight = 1.0;
        }

        public BaseMagicMushroom(Serial serial)
            : base(serial)
        {
        }

        public virtual bool Apply(Mobile from)
        {
            bool applied = Spells.SpellHelper.AddStatOffset(from, Type, Bonus, TimeSpan.FromMinutes(1.0));

            if (!applied)
                from.SendLocalizedMessage(502173); // You are already under a similar effect.

            return applied;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (Apply(from))
            {
                from.FixedEffect(0x375A, 10, 15);
                from.PlaySound(0x1E7);
                Delete();
            }
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

    // 0xd19 -  // big red one (3)
    // 0xd13    // cluster of little gray ones (1)
    // 0xd11    // clusted of little pointy white ones
    // 0xdoe    // small clusted of little pointy white ones
    // 0xd19    // big white one (2)
    // 0xd12    // cluster of little white ones
    // 0xd16    // small cluster of red ones
    // 0xd17    // single flattish one
    public class PointyMushroom : BaseMagicMushroom
    {
        public override int Bonus { get { return 10; } }
        public override StatType Type { get { return StatType.Int; } }

        [Constructable]
        public PointyMushroom()
            : base(Utility.RandomList(0xd11, 0xd0e))
        {
        }

        public PointyMushroom(Serial serial)
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

    public class FlatMushroom : BaseMagicMushroom
    {
        public override int Bonus { get { return 10; } }
        public override StatType Type { get { return StatType.Dex; } }

        [Constructable]
        public FlatMushroom()
            : base(0xd17)
        {
        }

        public FlatMushroom(Serial serial)
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

    public class RedMushroom : BaseMagicMushroom
    {
        public override int Bonus { get { return 10; } }
        public override StatType Type { get { return StatType.Str; } }

        [Constructable]
        public RedMushroom()
            : base(Utility.RandomList(0xd16, 0xd19))
        {
        }

        public RedMushroom(Serial serial)
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

    public class WhiteMushroom : BaseMagicMushroom
    {
        public override int Bonus { get { return 10; } }
        public override StatType Type { get { return (StatType)Utility.RandomList((int)StatType.Str, (int)StatType.Int, (int)StatType.Dex); } }

        [Constructable]
        public WhiteMushroom()
            : base(Utility.RandomList(0xd19, 0xd12, 0xd13))
        {
        }

        public WhiteMushroom(Serial serial)
            : base(serial)
        {
        }

        public override bool Apply(Mobile from)
        {
            from.Stam += 10;
            return true;
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